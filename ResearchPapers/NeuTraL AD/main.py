import torch
import torch.nn as nn
import torch.nn.functional as F
from torch.utils.data import DataLoader, TensorDataset
from sklearn.metrics import roc_auc_score, average_precision_score
import numpy as np
from model import NeuTraLAD

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
    for batch in loader:
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
    # Fake data: 800 inliers + 200 outliers, 30-dim features
    X = torch.randn(1000, 30)
    y = torch.zeros(1000)
    y[800:] = 1                          # last 200 are anomalies

    train_data = TensorDataset(X[:800], y[:800])          # train on inliers only
    test_data  = TensorDataset(X, y)                      # test on everything

    train_loader = DataLoader(train_data, batch_size=64, shuffle=True)
    test_loader  = DataLoader(test_data,  batch_size=64)

    model = NeuTraLAD(input_dim=30, hidden_dim=64, temperature=0.1, K=11)
    scores, labels = train_neutralad(model, train_loader, test_loader, epochs=50)