import torch
import pandas as pd
from trainer import NeuTraLADTrainer
from sklearn.preprocessing import StandardScaler
from sklearn.model_selection import train_test_split
from torch.utils.data import DataLoader, TensorDataset

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

    auc, ap, _scores, _labels = trainer.evaluate(test_loader)
    print(f"Epoch {-1} | Loss: -- | AUC: {auc:.4f} | AP: {ap:.4f}")
    scores, labels = trainer.train(train_loader, test_loader)