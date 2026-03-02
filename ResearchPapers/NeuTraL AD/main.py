import torch
import numpy as np
import pandas as pd
from tqdm import tqdm
from model import NeuTraLAD
from sklearn.preprocessing import StandardScaler
from sklearn.model_selection import train_test_split
from torch.utils.data import DataLoader, TensorDataset
from sklearn.metrics import roc_auc_score, average_precision_score

# ── Loss function (doubles as anomaly score) ────────────────────────────────
def neutralad_loss(scores, eval=False):
    """
    scores: (B,) per-sample anomaly scores from model.forward()
    eval:   if True, return raw scores for AUC; if False, return mean for backprop
    """
    if eval:
        return scores
    return scores.mean()


# ── Training loop ────────────────────────────────────────────────────────────
def train_epoch(model, loader, optimizer, device):
    model.train()
    total_loss = 0
    for batch in tqdm(loader):
        x = batch[0].to(device)
        scores = model(x)               # (B,) per-sample DCL scores
        loss = neutralad_loss(scores)   # scalar mean
        optimizer.zero_grad()
        loss.backward()
        optimizer.step()
        total_loss += loss.item() * x.size(0)
    return total_loss / len(loader.dataset)


# ── Evaluation ───────────────────────────────────────────────────────────────
def evaluate(model, loader, device):
    model.eval()
    all_scores, all_labels = [], []
    with torch.no_grad():
        for batch in loader:
            x, y = batch
            x = x.to(device)
            scores = model(x)                        # (B,)
            all_scores.append(scores.cpu().numpy())
            all_labels.append(y.numpy())

    all_scores = np.concatenate(all_scores)
    all_labels = np.concatenate(all_labels)
    auc = roc_auc_score(all_labels, all_scores)
    ap  = average_precision_score(all_labels, all_scores)
    return auc, ap, all_scores, all_labels


# ── Main training script ─────────────────────────────────────────────────────
def train_neutralad(
    model,
    train_loader,
    test_loader,
    epochs=50,
    lr=1e-3,
    device='cuda' if torch.cuda.is_available() else 'cpu'
):
    model = model.to(device)
    optimizer = torch.optim.Adam(model.parameters(), lr=lr)
    scheduler = torch.optim.lr_scheduler.CosineAnnealingLR(optimizer, T_max=epochs)

    best_auc = 0
    for epoch in range(1, epochs + 1):
        train_loss = train_epoch(model, train_loader, optimizer, device)
        scheduler.step()

        auc, ap, scores, labels = evaluate(model, test_loader, device)
        best_auc = max(best_auc, auc)

        if epoch % 5 == 0:
            print(f"Epoch {epoch:3d} | Loss: {train_loss:.4f} | AUC: {auc:.4f} | AP: {ap:.4f}")

    print(f"\nBest AUC: {best_auc:.4f}")
    return scores, labels

# ── Example usage ─────────────────────────────────────────────────────────────
if __name__ == "__main__":

    df = pd.read_csv("ResearchPapers/NeuTraL AD/creditcard.csv")
    df.drop(columns=['Time'], inplace=True)
    X = df.drop(columns=["Class"]).values
    y = df["Class"].values

    scaler = StandardScaler()
    X = scaler.fit_transform(X)
    X_train, X_test, y_train, y_test = train_test_split(
        X,
        y,
        test_size=0.2,
        stratify=y,        # ← ensures fraud cases in both sets
        random_state=42
    )

    # ── Convert to PyTorch tensors ──────────────────────
    X_train = torch.tensor(X_train, dtype=torch.float32)
    y_train = torch.tensor(y_train, dtype=torch.float32)

    X_test  = torch.tensor(X_test,  dtype=torch.float32)
    y_test  = torch.tensor(y_test,  dtype=torch.float32)

    # ── Train ONLY on normal samples (unsupervised setup) ──
    normal_mask = (y_train == 0)
    train_data = TensorDataset(X_train[normal_mask], y_train[normal_mask])

    # Test on full distribution
    test_data = TensorDataset(X_test, y_test)

    train_loader = DataLoader(train_data, batch_size=256, shuffle=True)
    test_loader  = DataLoader(test_data,  batch_size=256)

    # ── Model ────────────────────────────────────────────
    input_dim = X_train.shape[1]

    model = NeuTraLAD(
        input_dim=input_dim,
        hidden_dim=64,
        temperature=0.1,
        K=11
    )

    scores, labels = train_neutralad(
        model,
        train_loader,
        test_loader,
        epochs=50,
        device='cuda'
    )