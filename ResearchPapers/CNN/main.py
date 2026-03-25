from torchvision import datasets, transforms
from torch.utils.data import DataLoader
from model import CNN
from trainer import ClassifierTrainer

if __name__ == "__main__":
    train_transform = transforms.Compose([
        transforms.RandomHorizontalFlip(),
        transforms.RandomRotation(45),
        transforms.RandomAutocontrast(),
        transforms.Resize((64, 64)),
        transforms.ToTensor(),
        transforms.Normalize((0.5,), (0.5,))
    ])

    test_transform = transforms.Compose([
        transforms.Resize((64, 64)),
        transforms.ToTensor(),
        transforms.Normalize((0.5,), (0.5,))
    ])

    train_dataset = datasets.MNIST(root="./data", train=True, download=True, transform=train_transform)
    test_dataset = datasets.MNIST(root="./data", train=False, download=True, transform=test_transform)

    train_loader = DataLoader(train_dataset, batch_size=64, shuffle=True, num_workers=2)
    test_loader = DataLoader(test_dataset, batch_size=64, shuffle=False, num_workers=2)

    cnn = CNN((1, 64, 64), [16, 32], [128], 10, dropout=0.1)
    trainer = ClassifierTrainer(cnn)
    scores, labels = trainer.train(train_loader, test_loader)