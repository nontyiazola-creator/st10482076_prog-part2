using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Microsoft.Win32;

namespace CybersecurityChatbott
{
    public partial class MainWindow : Window
    {
        private ChatBot _chatBot;

        public MainWindow()
        {
            InitializeComponent();

            // Play startup sound once (non-blocking)
            try
            {
                AudioPlayer.PlayGreeting(); // MUST BE HERE
            }
            catch
            {
                // ignore audio failures on startup
            }

            // show ASCII header art in the chat display
            AppendHeaderArt();

            _chatBot = new ChatBot();
            AppendBotMessage(_chatBot.GetGreeting());
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void txtUserInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        private void SendMessage()
        {
            string userInput = txtUserInput.Text;

            if (string.IsNullOrWhiteSpace(userInput))
                return;

            AppendUserMessage(userInput);

            string response = _chatBot.ProcessInput(userInput);

            AppendBotMessage(response);

            AudioPlayer.Speak(response);

            txtUserInput.Clear();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            rtbChatDisplay.Document.Blocks.Clear();
            // optionally re-show greeting
            AppendBotMessage(_chatBot.GetGreeting());
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string fileName = string.IsNullOrEmpty(_chatBot.UserName) ? "cyber_chat_conversation.txt" : $"{_chatBot.UserName}_conversation.txt";
                string path = System.IO.Path.Combine(docs, fileName);

                var range = new TextRange(rtbChatDisplay.Document.ContentStart, rtbChatDisplay.Document.ContentEnd);
                File.WriteAllText(path, range.Text);

                AppendBotMessage($"Conversation saved to: {path}");
            }
            catch (Exception ex)
            {
                AppendBotMessage("Failed to save conversation: " + ex.Message);
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string path = System.IO.Path.Combine(docs, "cyber_chat_conversation.txt");
                if (!File.Exists(path))
                {
                    AppendBotMessage("No saved conversation found.");
                    return;
                }

                string text = File.ReadAllText(path);
                // clear current document
                rtbChatDisplay.Document.Blocks.Clear();

                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("You:"))
                    {
                        AppendUserMessage(trimmed.Substring(4).Trim());
                    }
                    else if (trimmed.StartsWith("Bot:"))
                    {
                        AppendBotMessage(trimmed.Substring(4).Trim());
                    }
                    else
                    {
                        AppendBotMessage(trimmed);
                    }
                }

                AppendBotMessage($"Conversation loaded from: {path}");
            }
            catch (Exception ex)
            {
                AppendBotMessage("Failed to load conversation: " + ex.Message);
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    FileName = string.IsNullOrEmpty(_chatBot.UserName) ? "conversation.txt" : $"{_chatBot.UserName}_conversation.txt"
                };

                if (dlg.ShowDialog() == true)
                {
                    var range = new TextRange(rtbChatDisplay.Document.ContentStart, rtbChatDisplay.Document.ContentEnd);
                    File.WriteAllText(dlg.FileName, range.Text);
                    AppendBotMessage($"Conversation exported to: {dlg.FileName}");
                }
            }
            catch (Exception ex)
            {
                AppendBotMessage("Failed to export conversation: " + ex.Message);
            }
        }

        private void AppendUserMessage(string message)
        {
            AppendBubble(message, isUser: true);
        }
        private void AppendHeaderArt()
        {
            // Create a compact title header and insert it at the top of the chat display
            var headerBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF16324A")),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8),
                Margin = new Thickness(6),
                MaxWidth = 520,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var headerText = new TextBlock
            {
                Text = "🔐 CYBERSECURITY AWARENESS BOT 🔐\n— Stay safe, stay aware —",
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };

            headerBorder.Child = headerText;

            var container = new BlockUIContainer(headerBorder) { Margin = new Thickness(0) };

            // Insert header at the very top so it remains the first block in the conversation
            var doc = rtbChatDisplay.Document;
            if (doc.Blocks.FirstBlock != null)
            {
                doc.Blocks.InsertBefore(doc.Blocks.FirstBlock, container);
            }
            else
            {
                doc.Blocks.Add(container);
            }
        }


        private void AppendBotMessage(string message)
        {
            AppendBubble(message, isUser: false);
        }

        private void AppendBubble(string message, bool isUser)
        {
            var border = new Border
            {
                Background = isUser ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2B6FA5")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1E3A5F")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                MaxWidth = 480,
                Margin = new Thickness(5)
            };

            var stack = new StackPanel();

            var txt = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                FontSize = 14
            };

            var time = new TextBlock
            {
                Text = DateTime.Now.ToString("g"),
                FontSize = 10,
                Foreground = Brushes.LightGray,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 6, 0, 0)
            };

            stack.Children.Add(txt);
            stack.Children.Add(time);
            border.Child = stack;

            var container = new BlockUIContainer(border)
            {
                Margin = new Thickness(0)
            };

            // adjust alignment
            if (isUser)
                border.HorizontalAlignment = HorizontalAlignment.Right;
            else
                border.HorizontalAlignment = HorizontalAlignment.Left;

            // add subtle drop shadow and entrance animation
            border.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 10,
                Opacity = 0.28,
                ShadowDepth = 3
            };

            border.Opacity = 0;
            var translate = new TranslateTransform(isUser ? 20 : -20, 0);
            border.RenderTransform = translate;

            rtbChatDisplay.Document.Blocks.Add(container);
            rtbChatDisplay.ScrollToEnd();

            // Animate opacity and slide-in
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            var slide = new DoubleAnimation(isUser ? 20 : -20, 0, TimeSpan.FromMilliseconds(320)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };

            border.BeginAnimation(UIElement.OpacityProperty, fade);
            translate.BeginAnimation(TranslateTransform.XProperty, slide);
        }
    }
}
