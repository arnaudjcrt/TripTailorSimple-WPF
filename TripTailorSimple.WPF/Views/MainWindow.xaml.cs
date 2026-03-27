using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TripTailorSimple.WPF.Models;
using TripTailorSimple.WPF.Services;

namespace TripTailorSimple.WPF.Views
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;
        private readonly ServiceDestinations _serviceDestinations;
        private readonly ServiceMeteo _serviceMeteo;
        private readonly ServiceWikipedia _serviceWikipedia;
        private readonly ServicePays _servicePays;
        private readonly ServiceSuggestionsVoyage _serviceSuggestionsVoyage;
        private readonly ServiceRechercheVoyage _serviceRechercheVoyage;
        private readonly ServiceNavigateur _serviceNavigateur;

        private List<PropositionVoyage> _resultatsActuels = new();
        private PropositionVoyage? _voyageActuel;

        public MainWindow()
        {
            InitializeComponent();

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TripTailorSimple-WPF/1.0");

            _serviceDestinations = new ServiceDestinations();
            _serviceMeteo = new ServiceMeteo(_httpClient);
            _serviceWikipedia = new ServiceWikipedia(_httpClient);
            _servicePays = new ServicePays(_httpClient);
            _serviceSuggestionsVoyage = new ServiceSuggestionsVoyage();
            _serviceNavigateur = new ServiceNavigateur();

            _serviceRechercheVoyage = new ServiceRechercheVoyage(
                _serviceDestinations,
                _serviceMeteo,
                _serviceWikipedia,
                _servicePays,
                _serviceSuggestionsVoyage);

            MettreAJourBudget();
            AfficherRecherche();
        }

        #region Navigation

        private void AfficherRecherche()
        {
            SectionRecherche.Visibility = Visibility.Visible;
            SectionResultats.Visibility = Visibility.Collapsed;
            SectionDetail.Visibility = Visibility.Collapsed;

            TxtTitreSection.Text = "Recherche";
            TxtSousTitreSection.Text = "Définis ton voyage idéal";
            TxtResumeEtat.Text = "Choisis tes critères puis lance la recherche.";
        }

        private void AfficherResultats()
        {
            SectionRecherche.Visibility = Visibility.Collapsed;
            SectionResultats.Visibility = Visibility.Visible;
            SectionDetail.Visibility = Visibility.Collapsed;

            TxtTitreSection.Text = "Résultats";
            TxtSousTitreSection.Text = "Destinations proposées";
            TxtResumeEtat.Text = $"{_resultatsActuels.Count} proposition(s) trouvée(s).";
        }

        private void AfficherDetail()
        {
            SectionRecherche.Visibility = Visibility.Collapsed;
            SectionResultats.Visibility = Visibility.Collapsed;
            SectionDetail.Visibility = Visibility.Visible;

            TxtTitreSection.Text = "Détail du voyage";
            TxtSousTitreSection.Text = "Vue complète de la destination";
            TxtResumeEtat.Text = _voyageActuel == null
                ? "Aucun voyage sélectionné."
                : $"{_voyageActuel.Ville}, {_voyageActuel.Pays}";
        }

        private void BtnMenuRecherche_Click(object sender, RoutedEventArgs e)
        {
            AfficherRecherche();
        }

        private void BtnMenuResultats_Click(object sender, RoutedEventArgs e)
        {
            if (_resultatsActuels.Count == 0)
            {
                MessageBox.Show("Lance d'abord une recherche.", "TripTailor", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AfficherResultats();
        }

        private void BtnMenuDetail_Click(object sender, RoutedEventArgs e)
        {
            if (_voyageActuel == null)
            {
                MessageBox.Show("Sélectionne d'abord un voyage.", "TripTailor", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AfficherDetail();
        }

        private void BtnRetourRecherche_Click(object sender, RoutedEventArgs e)
        {
            AfficherRecherche();
        }

        private void BtnRetourResultats_Click(object sender, RoutedEventArgs e)
        {
            AfficherResultats();
        }

        private void BtnOuvrirChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fenetreChat = new ChatWindow();
                fenetreChat.Owner = this;
                fenetreChat.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir le chatbot : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Recherche

        private async void BtnRechercher_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnRechercher.IsEnabled = false;
                BtnRechercher.Content = "Recherche...";

                var critere = LireCritereRecherche();

                _resultatsActuels = await _serviceRechercheVoyage.RechercherVoyagesAsync(critere);

                ConstruireCartesResultats(_resultatsActuels);

                TxtNombreResultats.Text = $"{_resultatsActuels.Count} destination(s) trouvée(s)";
                AfficherResultats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur pendant la recherche : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnRechercher.IsEnabled = true;
                BtnRechercher.Content = "Trouver mon voyage";
            }
        }

        private CritereRecherche LireCritereRecherche()
        {
            var climat = ((ComboBoxItem)ComboClimat.SelectedItem)?.Content?.ToString() ?? "Tempéré";
            var styleVoyage = ((ComboBoxItem)ComboStyleVoyage.SelectedItem)?.Content?.ToString() ?? "Confort";

            var regions = new List<string>();

            if (ChkEurope.IsChecked == true) regions.Add("Europe");
            if (ChkAsie.IsChecked == true) regions.Add("Asie");
            if (ChkAfrique.IsChecked == true) regions.Add("Afrique");
            if (ChkAmeriques.IsChecked == true) regions.Add("Amériques");
            if (ChkOceanie.IsChecked == true) regions.Add("Océanie");

            return new CritereRecherche
            {
                Climat = climat,
                StyleVoyage = styleVoyage,
                Budget = (int)SliderBudget.Value,
                NombreJours = int.Parse(TxtNombreJours.Text),
                Regions = regions
            };
        }

        #endregion

        #region Résultats

        private void ConstruireCartesResultats(List<PropositionVoyage> voyages)
        {
            PanelResultats.Children.Clear();

            if (voyages.Count == 0)
            {
                PanelResultats.Children.Add(new TextBlock
                {
                    Text = "Aucun résultat pour ces critères.",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7A76")),
                    Margin = new Thickness(10)
                });

                return;
            }

            foreach (var voyage in voyages)
            {
                var carte = new Border
                {
                    Width = 320,
                    Margin = new Thickness(0, 0, 22, 22),
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(18),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3EAEA")),
                    BorderThickness = new Thickness(1)
                };

                var racine = new StackPanel();

                var image = new Image
                {
                    Height = 210,
                    Stretch = Stretch.Fill
                };
                ChargerImage(image, voyage.UrlImage);

                var conteneurImage = new Border
                {
                    CornerRadius = new CornerRadius(18, 18, 0, 0),
                    Child = image,
                    ClipToBounds = true
                };

                racine.Children.Add(conteneurImage);

                var contenu = new StackPanel
                {
                    Margin = new Thickness(18)
                };

                var grilleTitre = new Grid();
                grilleTitre.ColumnDefinitions.Add(new ColumnDefinition());
                grilleTitre.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var blocGauche = new StackPanel();
                blocGauche.Children.Add(new TextBlock
                {
                    Text = voyage.Ville,
                    FontSize = 22,
                    FontWeight = FontWeights.ExtraBold
                });
                blocGauche.Children.Add(new TextBlock
                {
                    Text = voyage.Pays,
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7A76"))
                });

                var blocDroite = new StackPanel();
                blocDroite.Children.Add(new TextBlock
                {
                    Text = $"{voyage.PrixTotal} €",
                    FontSize = 20,
                    FontWeight = FontWeights.Black,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#006B5F")),
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                blocDroite.Children.Add(new TextBlock
                {
                    Text = $"{voyage.TemperatureMoyenne:0.#} °C",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7A76")),
                    HorizontalAlignment = HorizontalAlignment.Right
                });

                Grid.SetColumn(blocGauche, 0);
                Grid.SetColumn(blocDroite, 1);

                grilleTitre.Children.Add(blocGauche);
                grilleTitre.Children.Add(blocDroite);

                contenu.Children.Add(grilleTitre);

                var panelTags = new WrapPanel
                {
                    Margin = new Thickness(0, 12, 0, 14)
                };

                foreach (var tag in voyage.Etiquettes.Take(4))
                {
                    panelTags.Children.Add(new Border
                    {
                        Margin = new Thickness(0, 0, 8, 8),
                        Padding = new Thickness(8, 4, 8, 4),
                        CornerRadius = new CornerRadius(10),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BACAC5")),
                        BorderThickness = new Thickness(1),
                        Child = new TextBlock
                        {
                            Text = tag,
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7A76"))
                        }
                    });
                }

                contenu.Children.Add(panelTags);

                contenu.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(voyage.Description)
                        ? "Destination idéale selon tes critères."
                        : voyage.Description,
                    TextWrapping = TextWrapping.Wrap,
                    MaxHeight = 58,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7A76"))
                });

                var boutonVoir = new Button
                {
                    Content = "Voir le voyage",
                    Margin = new Thickness(0, 16, 0, 0),
                    Height = 42,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#006B5F")),
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.Transparent,
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = voyage
                };
                boutonVoir.Click += BtnVoirVoyage_Click;

                contenu.Children.Add(boutonVoir);

                racine.Children.Add(contenu);
                carte.Child = racine;

                PanelResultats.Children.Add(carte);
            }
        }

        private async void BtnVoirVoyage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button bouton || bouton.Tag is not PropositionVoyage voyage)
                return;

            _voyageActuel = voyage;
            await ChargerDetailAsync(voyage);
            AfficherDetail();
        }

        #endregion

        #region Détail

        private async Task ChargerDetailAsync(PropositionVoyage voyage)
        {
            TxtVillePays.Text = $"{voyage.Ville}, {voyage.Pays}";
            TxtRegionPays.Text = $"{voyage.Region} • budget estimé {voyage.PrixTotal} €";
            TxtDescription.Text = string.IsNullOrWhiteSpace(voyage.Description)
                ? "Aucune description disponible."
                : voyage.Description;

            ChargerImage(ImgDestination, voyage.UrlImage);

            ListeBudget.ItemsSource = new List<string>
            {
                $"Vol : {voyage.PrixVol} €",
                $"Hôtel : {voyage.PrixHotel} €",
                $"Activités : {voyage.PrixActivites} €",
                $"Total : {voyage.PrixTotal} €"
            };

            ListeActivitesPayantes.ItemsSource = voyage.Activites;
            ListeIdeesGratuites.ItemsSource = voyage.IdeesGratuites;

            PanelTags.Children.Clear();
            foreach (var tag in voyage.Etiquettes)
            {
                PanelTags.Children.Add(new Border
                {
                    Margin = new Thickness(0, 0, 8, 8),
                    Padding = new Thickness(10, 6, 10, 6),
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEF8F6")),
                    Child = new TextBlock
                    {
                        Text = tag,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#006B5F"))
                    }
                });
            }

            TxtMeteoActuelle.Text = "Chargement météo...";
            ListePrevisions.ItemsSource = null;

            var previsions = await _serviceMeteo.RecupererPrevisionsJournalieresAsync(
                voyage.Latitude,
                voyage.Longitude,
                5);

            if (previsions.Count == 0)
            {
                TxtMeteoActuelle.Text = $"{voyage.TemperatureMoyenne:0.#} °C";
                ListePrevisions.ItemsSource = new List<string> { "Prévisions indisponibles" };
            }
            else
            {
                TxtMeteoActuelle.Text = $"{previsions[0].Temperature:0.#} °C - {previsions[0].ResumeMeteo}";
                ListePrevisions.ItemsSource = previsions
                    .Select(p => $"{p.Jour} - {p.Temperature:0.#} °C - {p.ResumeMeteo}")
                    .ToList();
            }
        }

        private void BtnVoirVols_Click(object sender, RoutedEventArgs e)
        {
            if (_voyageActuel != null)
                _serviceNavigateur.OuvrirUrl(_voyageActuel.UrlVol);
        }

        private void BtnVoirHotel_Click(object sender, RoutedEventArgs e)
        {
            if (_voyageActuel != null)
                _serviceNavigateur.OuvrirUrl(_voyageActuel.UrlHotel);
        }

        private void BtnVoirActivites_Click(object sender, RoutedEventArgs e)
        {
            if (_voyageActuel != null)
                _serviceNavigateur.OuvrirUrl(_voyageActuel.UrlActivites);
        }

        #endregion

        #region UI

        private void BtnReinitialiser_Click(object sender, RoutedEventArgs e)
        {
            ComboClimat.SelectedIndex = 1;
            ComboStyleVoyage.SelectedIndex = 1;
            SliderBudget.Value = 2000;
            TxtNombreJours.Text = "7";

            ChkEurope.IsChecked = false;
            ChkAsie.IsChecked = false;
            ChkAfrique.IsChecked = false;
            ChkAmeriques.IsChecked = false;
            ChkOceanie.IsChecked = false;

            MettreAJourBudget();
            AfficherRecherche();
        }

        private void SliderBudget_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MettreAJourBudget();
        }

        private void MettreAJourBudget()
        {
            if (TxtBudget != null)
                TxtBudget.Text = $"{(int)SliderBudget.Value} €";
        }

        private void BtnMoinsJour_Click(object sender, RoutedEventArgs e)
        {
            int jours = int.Parse(TxtNombreJours.Text);
            if (jours > 2)
                TxtNombreJours.Text = (jours - 1).ToString();
        }

        private void BtnPlusJour_Click(object sender, RoutedEventArgs e)
        {
            int jours = int.Parse(TxtNombreJours.Text);
            if (jours < 30)
                TxtNombreJours.Text = (jours + 1).ToString();
        }

        private static void ChargerImage(Image imageControl, string? url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    imageControl.Source = null;
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                imageControl.Source = bitmap;
            }
            catch
            {
                imageControl.Source = null;
            }
        }

        #endregion
    }
}