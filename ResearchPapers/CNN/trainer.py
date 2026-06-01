import copy
from sklearn.metrics import roc_auc_score
import torch
from tqdm import tqdm
import torch.nn as nn
from abc import ABC, abstractmethod

class BaseTrainer(ABC):
    def __init__(self, 
                 model: nn.Module, 
                 epochs: int = 10, 
                 optimizer_cls: type[torch.optim.Optimizer] = torch.optim.Adam, 
                 lr: float = 1e-3, 
                 grad_clip: float | None = 1.0,
                 maximize_metric: bool = True,
                 total_patience: int = 3
                 ):
        self.device = 'cuda' if torch.cuda.is_available() else 'cpu'
        self.mode = 'max' if maximize_metric else 'min'
        self.model = model
        self.grad_clip = grad_clip
        self.model.to(self.device)
        self.optimizer = optimizer_cls(self.model.parameters(), lr=lr)
        self.scheduler = torch.optim.lr_scheduler.ReduceLROnPlateau(
            self.optimizer, mode=self.mode, patience=5, factor=0.5)
        self.epochs = epochs
        self.total_patience = total_patience
    
    @abstractmethod
    def criterion(self, pred_y, true_y):
        pass
        
    def epoch(self, dataloader):
        self.model.train()
        total_loss = 0
        for batch in tqdm(dataloader, leave=False):
            x, y = batch
            x = x.to(self.device)
            y = y.to(self.device)
            self.optimizer.zero_grad()                          # Clear gradients from previous batch
            
            logits = self.model(x)                              # Compute model output scores
            loss = self.criterion(logits, y)

            loss.backward()                                     # Calculates parameters' gradients (backpropagation)
            if self.grad_clip is not None:
                torch.nn.utils.clip_grad_norm_(
                    self.model.parameters(), self.grad_clip)    # Clips gradient norm to prevent exploding gradients
            self.optimizer.step()                               # Update parameters to minimise loss (opposite gradient direction)

            total_loss += loss.item() * x.size(0)
        return total_loss/len(dataloader.dataset)

    @abstractmethod
    def compute_metric(self, pred_y, true_y):
        pass

    def evaluate(self, dataloader):
        self.model.eval()
        all_scores, all_labels = [], []
        with torch.no_grad():
            for batch in dataloader:
                x, y = batch
                x = x.to(self.device)
                scores = self.model(x)
                all_scores.append(scores.cpu())
                all_labels.append(y)

        all_scores = torch.cat(all_scores)
        all_labels = torch.cat(all_labels)
        return self.compute_metric(all_scores, all_labels), all_scores, all_labels

    def train(self, train_loader, val_loader):
        
        best_statistic = -float('inf') if self.mode=='max' else float('inf')
        patience_counter = 0
        best_model_state = None
        best_scores, best_labels = None, None
        for epoch in range(self.epochs):
            train_loss = self.epoch(train_loader)
            statistic, scores, labels = self.evaluate(val_loader)
            self.model.train()
            
            is_better = (statistic > best_statistic if self.mode == 'max' else statistic < best_statistic)
            if is_better:
                patience_counter = 0
                best_model_state = copy.deepcopy(self.model.state_dict())
                best_statistic = statistic
                best_scores, best_labels = scores, labels
            else:
                patience_counter += 1
                if patience_counter > self.total_patience:
                    print(f"Early stopping at epoch {epoch}")
                    break
            self.scheduler.step(statistic)
            # statistics printout
            print(f"Epoch {epoch:3d} | Loss: {train_loss:.4f} | Statistic: {statistic:.4f}")
        print(f"Best Statistic: {best_statistic:.4f}")
        if best_model_state is not None:
            self.model.load_state_dict(best_model_state)
        return best_scores, best_labels


class ClassifierTrainer(BaseTrainer):
    
    def criterion(self, pred_y, true_y):
        return nn.CrossEntropyLoss()(pred_y, true_y)

    def compute_metric(self, pred_y, true_y):
        probs = torch.softmax(pred_y, dim=1)
        return roc_auc_score(true_y.numpy(), probs.numpy(), multi_class='ovr')