using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileCrypt.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<string> files = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        public ICommand EncryptCommand { get => new RelayCommand(async obj => await EncryptCommandClicked(obj)); }
        public ICommand DecryptCommand { get => new RelayCommand(async obj => await DecryptCommandClicked(obj)); }
        public ICommand BrowseFilesCommand { get => new RelayCommand(async obj => await BrowseFilesCommandClicked(obj)); }

        public ObservableCollection<string> Files
        {
            get => files; set
            {
                files = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Files"));
            }
        }

        private async Task DecryptCommandClicked(object obj)
        {
            var passwordBox = obj as PasswordBox;
            var password = passwordBox?.Password ?? string.Empty;

            if (string.IsNullOrEmpty(password))
            {
                if (MessageBoxResult.No == MessageBox.Show("Do you want to excrypt the files without password", "No Password", MessageBoxButton.YesNo, MessageBoxImage.Question))
                    return;
            }

            foreach (var file in files)
            {
                await DecryptFileAsync(password, file);
            }
        }

        private async Task EncryptCommandClicked(object obj)
        {
            var passwordBox = obj as PasswordBox;
            var password = passwordBox?.Password ?? string.Empty;

            if (string.IsNullOrEmpty(password))
            {
                if (MessageBoxResult.No == MessageBox.Show("Do you want to excrypt the files without password", "No Password", MessageBoxButton.YesNo, MessageBoxImage.Question))
                    return;
            }

            foreach (var file in files)
            {
                await EncryptFileAsync(password, file);
            }
        }
        private async Task BrowseFilesCommandClicked(object obj)
        {
            Files.Clear();
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() ?? false)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    Files.Add(file);
                }
            }

            await Task.CompletedTask;
        }

        public Task DecryptFileAsync(string password, string srcFilename)
        {
            var aes = GetAES(password);
            string decryptedDir = Path.Combine(Directory.GetParent(srcFilename)?.FullName ?? string.Empty, "decrypted");

            if (!Directory.Exists(decryptedDir))
                Directory.CreateDirectory(Path.Combine(decryptedDir));

            ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
            using (var dest = new FileStream(Path.Combine(decryptedDir, Path.GetFileNameWithoutExtension(srcFilename)), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var cryptoStream = new CryptoStream(dest, transform, CryptoStreamMode.Write))
                {
                    try
                    {
                        using (var source = new FileStream(srcFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            source.CopyTo(cryptoStream);
                        }
                    }
                    catch (CryptographicException exception)
                    {
                        throw new ApplicationException("Decryption failed.", exception);
                    }
                }
            }

            return Task.CompletedTask;
        }



        public Task EncryptFileAsync(string password, string srcFilename)
        {
            var aes = GetAES(password);

            string encryptedDir = Path.Combine(Directory.GetParent(srcFilename)?.FullName ?? string.Empty, "encrypted");

            if (!Directory.Exists(encryptedDir))
                Directory.CreateDirectory(Path.Combine(encryptedDir));

            ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV);
            using (var destination = new FileStream(Path.Combine(encryptedDir, Path.GetFileName(srcFilename) + ".enc"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var cryptoStream = new CryptoStream(destination, transform, CryptoStreamMode.Write))
                {
                    using (var source = new FileStream(srcFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        source.CopyTo(cryptoStream);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private Aes GetAES(string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] aesKey = SHA256.Create().ComputeHash(passwordBytes);
            byte[] aesIV = MD5.Create().ComputeHash(passwordBytes);
            var aes = Aes.Create();

            aes.Key = aesKey;
            aes.IV = aesIV;

            return aes;
        }
    }
}
