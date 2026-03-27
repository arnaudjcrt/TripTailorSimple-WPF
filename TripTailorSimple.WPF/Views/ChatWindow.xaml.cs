using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using TripTailorSimple.WPF.Models;
using TripTailorSimple.WPF.Services;

namespace TripTailorSimple.WPF.Views;

public partial class ChatWindow : Window
{
    private readonly GitHubAiService? _ai;

    public ChatWindow()
    {
        InitializeComponent();

        try
        {
            _ai = new GitHubAiService(new HttpClient());
            ChatBox.Text = "IA : Bonjour ! Pose-moi une question.\n\n";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Erreur d'initialisation",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            StatusText.Text = "Erreur de configuration";
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    private async void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        if (_ai == null)
            return;

        var userText = InputBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(userText))
            return;

        InputBox.Clear();

        ChatBox.Text += $"Vous : {userText}\n";
        StatusText.Text = "Envoi en cours...";
        SendButton.IsEnabled = false;

        try
        {
            var msgs = new List<ChatMessage>
            {
                new ChatMessage
                {
                    Role = "system",
                    Content = "Tu es un assistant utile, clair et précis."
                },
                new ChatMessage
                {
                    Role = "user",
                    Content = userText
                }
            };

            var reply = await _ai.ChatAsync(msgs);

            ChatBox.Text += $"IA : {reply}\n\n";
            StatusText.Text = "Réponse reçue";
        }
        catch (Exception ex)
        {
            ChatBox.Text += $"[Erreur] {ex.Message}\n\n";
            StatusText.Text = "Erreur API";
        }
        finally
        {
            SendButton.IsEnabled = true;
        }
    }
}