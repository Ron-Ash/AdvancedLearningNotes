import torch
import numpy as np
import pandas as pd
from tqdm import tqdm
from model import NeuTraLAD
from sklearn.preprocessing import StandardScaler
from sklearn.model_selection import train_test_split
from torch.utils.data import DataLoader, TensorDataset
from sklearn.metrics import roc_auc_score, average_precision_score

class NeuTraLADTrainer():
    def __init__(self, input_dim, hidden_dim, depth, temperature=1, K=11, epochs=10):
        self.device = 'cuda' if torch.cuda.is_available() else 'cpu'
        self.model = NeuTraLAD(input_dim, hidden_dim, depth, temperature, K).to(self.device)
        self.optimizer = torch.optim.Adam(self.model.parameters(), 1e-3)
        self.scheduler = torch.optim.lr_scheduler.ReduceLROnPlateau(self.optimizer, mode='max', patience=5, factor=0.5)
        self.epochs = epochs
    
    def epoch(self, train_loader):
        self.model.train()
        total_loss = 0
        for batch in tqdm(train_loader):
            x = batch[0].to(self.device)
            self.optimizer.zero_grad()
            scores = self.model(x)
            loss = scores.mean()
            loss.backward()
            torch.nn.utils.clip_grad_norm_(self.model.parameters(), max_norm=1.0)
            self.optimizer.step()
            total_loss += loss.item() * x.size(0)
        return total_loss / len(train_loader.dataset)

    def evaluate(self, train_loader):
        self.model.eval()
        all_scores, all_labels = [], []
        with torch.no_grad():
            for batch in train_loader:
                x, y = batch
                x = x.to(self.device)
                scores = self.model(x)
                all_scores.append(scores.cpu().numpy())
                all_labels.append(y.numpy())

        all_scores = np.concatenate(all_scores)
        all_labels = np.concatenate(all_labels)
        auc = roc_auc_score(all_labels, all_scores)
        ap  = average_precision_score(all_labels, all_scores)
        self.model.train()
        return auc, ap, all_scores, all_labels

    def train(self, train_loader, test_loader):
        self.model.train()
        best_auc = 0
        total_patience = 3
        patience_counter = 0
        best_scores, best_labels = None, None
        for epoch in range(self.epochs):
            train_loss = self.epoch(train_loader)
            auc, ap, scores, labels = self.evaluate(test_loader)
            if auc > best_auc:
                best_auc = auc
                best_scores, best_labels = scores, labels
            else:
                patience_counter += 1
                if patience_counter > total_patience:
                    print(f"Early stopping at epoch {epoch}")
                    break
            self.scheduler.step(auc)
            # statistics printout
            print(f"Epoch {epoch:3d} | Loss: {train_loss:.4f} | AUC: {auc:.4f} | AP: {ap:.4f}")
        print(f"\nBest AUC: {best_auc:.4f}")
        return best_scores, best_labels


if __name__ == "__main__":

    df = pd.read_csv("ResearchPapers/NeuTraL AD/creditcard.csv")
    df.drop(columns=['Time'], inplace=True)
    X = df.drop(columns=["Class"]).values
    y = df["Class"].values

    scaler = StandardScaler()
    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, stratify=y, random_state=42)
    X_train = scaler.fit_transform(X_train)
    X_test  = scaler.transform(X_test)

    # ── Convert to PyTorch tensors ──────────────────────
    X_train = torch.tensor(X_train, dtype=torch.float32)
    y_train = torch.tensor(y_train, dtype=torch.float32)

    X_test  = torch.tensor(X_test,  dtype=torch.float32)
    y_test  = torch.tensor(y_test,  dtype=torch.float32)

    # ── Train ONLY on normal samples (unsupervised setup) ──
    normal_mask = (y_train == 0)
    train_data = TensorDataset(X_train[normal_mask], y_train[normal_mask])

    # ── Train on all samples with unknown positives (unsupervised setup) ──
    # positive_mask = (y_train == 1)
    # y_train[positive_mask] = 0
    # train_data = TensorDataset(X_train, y_train)

    # Test on full distribution
    test_data = TensorDataset(X_test, y_test)

    train_loader = DataLoader(train_data, batch_size=256, shuffle=True)
    test_loader  = DataLoader(test_data,  batch_size=256)

    # ── Model ────────────────────────────────────────────
    input_dim = X_train.shape[1]

    trainer = NeuTraLADTrainer(
        input_dim=input_dim,
        hidden_dim=64,
        depth=4,
        temperature=0.5)

    scores, labels = trainer.train(train_loader, test_loader)