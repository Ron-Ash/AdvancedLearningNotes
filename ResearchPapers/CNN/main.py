from torchvision import datasets, transforms
from torch.utils.data import DataLoader, random_split
from model import CNN
from trainer import ClassifierTrainer

if __name__ == "__main__":
    base_transform = transforms.Compose([
        transforms.Resize((64, 64)),
        transforms.ToTensor(),
        transforms.Normalize((0.5,), (0.5,))
    ])

    train_transform = transforms.Compose([
        transforms.RandomHorizontalFlip(),
        transforms.RandomRotation(45),
        transforms.RandomAutocontrast(),
        *base_transform.transforms
    ])

    train_dataset = datasets.MNIST(root="./data", train=True, download=True, transform=train_transform)
    train_size = int(0.8 * len(train_dataset))
    val_size = len(train_dataset) - train_size
    train_subset, val_subset = random_split(train_dataset, [train_size, val_size])
    train_loader = DataLoader(train_subset, batch_size=64, shuffle=True, num_workers=2)
    val_loader = DataLoader(val_subset, batch_size=64, shuffle=False, num_workers=2)
    
    test_dataset = datasets.MNIST(root="./data", train=False, download=True, transform=base_transform)
    test_loader = DataLoader(test_dataset, batch_size=64, shuffle=False, num_workers=2)

    cnn = CNN((1, 64, 64), [16, 32], [128], 10, dropout=0.1)
    trainer = ClassifierTrainer(cnn)
    scores, labels = trainer.train(train_loader, val_loader)

    test_stat, _, _ = trainer.evaluate(test_loader)
    print("Test performance:", test_stat)