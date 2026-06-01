import copy
import torch
import numpy as np
from tqdm import tqdm
from model import NeuTraLAD
from sklearn.metrics import roc_auc_score, average_precision_score

class NeuTraLADTrainer():
    def __init__(self, input_dim, hidden_dim, depth, temperature=1, K=11, epochs=10):
        self.device = 'cuda' if torch.cuda.is_available() else 'cpu'
        self.model = NeuTraLAD(input_dim, hidden_dim, depth, temperature, K).to(self.device)
        self.model.to(self.device)
        self.optimizer = torch.optim.Adam(self.model.parameters(), 1e-3)
        self.scheduler = torch.optim.lr_scheduler.ReduceLROnPlateau(self.optimizer, mode='max', patience=5, factor=0.5)
        self.epochs = epochs
    
    def epoch(self, train_loader):
        self.model.train()
        total_loss = 0
        for batch in tqdm(train_loader):
            x = batch[0].to(self.device)
            self.optimizer.zero_grad()                  # Clear gradients from previous batch
            
            scores = self.model(x)                      # Compute model output scores
            loss = scores.mean()                        # Aggregate scores into a scalar loss

            loss.backward()                             # Calculates parameters' gradients (backpropagation)
            torch.nn.utils.clip_grad_norm_(
                self.model.parameters(), max_norm=1.0)  # Clips gradient norm to prevent exploding gradients
            self.optimizer.step()                       # Update parameters to minimise loss (opposite gradient direction)

            total_loss += loss.item() * x.size(0)
        return total_loss/len(train_loader.dataset)

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
        best_model_state = None
        best_scores, best_labels = None, None
        for epoch in range(self.epochs):
            train_loss = self.epoch(train_loader)
            auc, ap, scores, labels = self.evaluate(test_loader)
            if auc > best_auc:
                patience_counter = max(0, patience_counter-1)
                best_model_state = copy.deepcopy(self.model.state_dict())
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
        self.model.load_state_dict(best_model_state)
        return best_scores, best_labels