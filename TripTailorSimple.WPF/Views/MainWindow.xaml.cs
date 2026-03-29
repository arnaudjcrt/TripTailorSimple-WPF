using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TripTailorSimple.WPF.Models;
using TripTailorSimple.WPF.Modeles;
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
        private readonly ServiceRechercheLibre _serviceRechercheLibre;
        private readonly ServiceNavigateur _serviceNavigateur;

        private readonly HashSet<string> _regionsSelectionnees = new(StringComparer.OrdinalIgnoreCase);

        private List<PropositionVoyage> _resultatsActuels = new();
        private PropositionVoyage? _voyageActuel;
        private string _styleSelectionne = "Confort";

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

            _serviceRechercheLibre = new ServiceRechercheLibre(
                _httpClient,
                _serviceMeteo,
                _serviceWikipedia,
                _servicePays,
                _serviceSuggestionsVoyage);

            MettreAJourBudget();
            MettreAJourStyleActif();
            MettreAJourNavActif(BtnNavAccueil);
            AfficherAccueil();
        }

        #region Navigation

        private void AfficherAccueil()
        {
            SectionAccueil.Visibility = Visibility.Visible;
            SectionResultats.Visibility = Visibility.Collapsed;
            SectionDetail.Visibility = Visibility.Collapsed;
            MettreAJourNavActif(BtnNavAccueil);
        }

        private void AfficherResultats()
        {
            SectionAccueil.Visibility = Visibility.Collapsed;
            SectionResultats.Visibility = Visibility.Visible;
            SectionDetail.Visibility = Visibility.Collapsed;
            MettreAJourNavActif(BtnNavDestinations);
        }

        private void AfficherDetail()
        {
            SectionAccueil.Visibility = Visibility.Collapsed;
            SectionResultats.Visibility = Visibility.Collapsed;
            SectionDetail.Visibility = Visibility.Visible;
            MettreAJourNavActif(BtnNavDestinations);
        }

        private void MettreAJourNavActif(Button actif)
        {
            ReinitialiserNav(BtnNavAccueil);
            ReinitialiserNav(BtnNavDestinations);

            actif.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEF8FD"));
            actif.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#44B3E8"));
        }

        private void ReinitialiserNav(Button btn)
        {
            btn.Background = Brushes.Transparent;
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F7A86"));
        }

        private void BtnNavAccueil_Click(object sender, RoutedEventArgs e)
        {
            AfficherAccueil();
        }

        private void BtnNavDestinations_Click(object sender, RoutedEventArgs e)
        {
            if (_resultatsActuels.Count == 0)
            {
                MessageBox.Show("Lance d'abord une recherche.", "TripTailor", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AfficherResultats();
        }

        private void BtnRetourAccueil_Click(object sender, RoutedEventArgs e)
        {
            AfficherAccueil();
        }

        private void BtnRetourResultats_Click(object sender, RoutedEventArgs e)
        {
            if (_resultatsActuels.Count == 0)
            {
                AfficherAccueil();
                return;
            }

            AfficherResultats();
        }

        private void BtnOuvrirChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fenetreChat = new ChatWindow
                {
                    Owner = this
                };
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
                var texteLibre = TxtRechercheLibre.Text?.Trim() ?? string.Empty;

                List<PropositionVoyage> resultats;

                if (!string.IsNullOrWhiteSpace(texteLibre))
                {
                    resultats = await RechercherAvecTexteLibreAsync(texteLibre, critere);
                }
                else
                {
                    resultats = await _serviceRechercheVoyage.RechercherVoyagesAsync(critere);
                }

                if (ChkVolInclus.IsChecked != true)
                {
                    foreach (var item in resultats)
                    {
                        item.PrixTotal -= item.PrixVol;
                        item.PrixVol = 0;
                    }
                }

                if (ChkHotelInclus.IsChecked != true)
                {
                    foreach (var item in resultats)
                    {
                        item.PrixTotal -= item.PrixHotel;
                        item.PrixHotel = 0;
                    }
                }

                if (ChkActivitesPayantes.IsChecked != true)
                {
                    foreach (var item in resultats)
                    {
                        item.PrixTotal -= item.PrixActivites;
                        item.PrixActivites = 0;
                        item.Activites = new List<string>();
                    }
                }

                if (ChkIdeesGratuites.IsChecked != true)
                {
                    foreach (var item in resultats)
                    {
                        item.IdeesGratuites = new List<string>();
                    }
                }

                _resultatsActuels = resultats
                    .OrderBy(x => x.PrixTotal)
                    .ThenByDescending(x => x.Score)
                    .ToList();

                ConstruireCartesResultats(_resultatsActuels);
                TxtNombreResultats.Text = _resultatsActuels.Count switch
                {
                    0 => "0 destination trouvée",
                    1 => "1 destination trouvée",
                    _ => $"{_resultatsActuels.Count} destinations trouvées"
                };

                AfficherResultats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur pendant la recherche : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnRechercher.IsEnabled = true;
                BtnRechercher.Content = "Trouver ma destination";
            }
        }

        private async System.Threading.Tasks.Task<List<PropositionVoyage>> RechercherAvecTexteLibreAsync(string texteLibre, CritereRecherche critere)
        {
            var tout = await _serviceRechercheVoyage.RechercherVoyagesAsync(critere);

            var locaux = tout
                .Where(x =>
                    x.Ville.Contains(texteLibre, StringComparison.OrdinalIgnoreCase) ||
                    x.Pays.Contains(texteLibre, StringComparison.OrdinalIgnoreCase) ||
                    x.Region.Contains(texteLibre, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (locaux.Count > 0)
                return locaux;

            var rechercheLibre = await _serviceRechercheLibre.RechercherAsync(texteLibre, critere);
            if (rechercheLibre != null)
                return new List<PropositionVoyage> { rechercheLibre };

            return new List<PropositionVoyage>();
        }

        private CritereRecherche LireCritereRecherche()
        {
            var climat = ((ComboBoxItem)ComboClimat.SelectedItem)?.Content?.ToString() ?? "Tempéré";

            return new CritereRecherche
            {
                Climat = climat,
                StyleVoyage = _styleSelectionne,
                Budget = (int)SliderBudget.Value,
                NombreJours = LireNombreJours(),
                Regions = _regionsSelectionnees.ToList()
            };
        }

        private int LireNombreJours()
        {
            return int.TryParse(TxtNombreJours.Text, out int jours)
                ? Math.Max(2, Math.Min(30, jours))
                : 7;
        }

        #endregion

        #region Styles / régions / budget

        private void SliderBudget_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MettreAJourBudget();
        }

        private void MettreAJourBudget()
        {
            if (TxtBudgetLabel != null)
            {
                TxtBudgetLabel.Text = $"Budget total (€) : {(int)SliderBudget.Value} €";
            }
        }

        private void BtnMoinsJour_Click(object sender, RoutedEventArgs e)
        {
            int jours = LireNombreJours();
            if (jours > 2)
                TxtNombreJours.Text = (jours - 1).ToString();
        }

        private void BtnPlusJour_Click(object sender, RoutedEventArgs e)
        {
            int jours = LireNombreJours();
            if (jours < 30)
                TxtNombreJours.Text = (jours + 1).ToString();
        }

        private void BtnStyleEconomique_Click(object sender, RoutedEventArgs e)
        {
            _styleSelectionne = "Économique";
            MettreAJourStyleActif();
        }

        private void BtnStyleConfort_Click(object sender, RoutedEventArgs e)
        {
            _styleSelectionne = "Confort";
            MettreAJourStyleActif();
        }

        private void BtnStyleLuxe_Click(object sender, RoutedEventArgs e)
        {
            _styleSelectionne = "Luxe";
            MettreAJourStyleActif();
        }

        private void MettreAJourStyleActif()
        {
            AppliquerStyleBouton(BtnStyleEconomique, _styleSelectionne == "Économique");
            AppliquerStyleBouton(BtnStyleConfort, _styleSelectionne == "Confort");
            AppliquerStyleBouton(BtnStyleLuxe, _styleSelectionne == "Luxe");
        }

        private void AppliquerStyleBouton(Button btn, bool actif)
        {
            btn.Background = actif
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EAF7FD"))
                : Brushes.White;

            btn.BorderBrush = actif
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#56B9E9"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E5E2"));

            btn.Foreground = actif
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D98B9"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D2733"));

            btn.FontWeight = actif ? FontWeights.Bold : FontWeights.SemiBold;
        }

        private void BtnRegion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string region)
                return;

            if (_regionsSelectionnees.Contains(region))
            {
                _regionsSelectionnees.Remove(region);
                btn.Background = Brushes.White;
                btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E5E2"));
                btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D2733"));
            }
            else
            {
                _regionsSelectionnees.Add(region);
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEF8FD"));
                btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#56B9E9"));
                btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D98B9"));
            }
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
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F7A86")),
                    Margin = new Thickness(10)
                });
                return;
            }

            foreach (var voyage in voyages)
            {
                var carte = new Border
                {
                    Width = 340,
                    Margin = new Thickness(0, 0, 22, 22),
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(18),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E5E2")),
                    BorderThickness = new Thickness(1),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 16,
                        ShadowDepth = 3,
                        Opacity = 0.12,
                        Color = Colors.Black
                    }
                };

                var racine = new StackPanel();

                var zoneImage = new Grid
                {
                    Height = 190
                };

                var image = new Image
                {
                    Stretch = Stretch.Fill
                };
                ChargerImage(image, voyage.UrlImage);

                zoneImage.Children.Add(image);

                if (!string.IsNullOrWhiteSpace(voyage.UrlDrapeau))
                {
                    var flag = new Image
                    {
                        Width = 24,
                        Height = 18,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 10, 10, 0)
                    };
                    ChargerImage(flag, voyage.UrlDrapeau);
                    zoneImage.Children.Add(flag);
                }

                var bandeau = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(10)
                };

                bandeau.Children.Add(new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 3, 8, 3),
                    Margin = new Thickness(0, 0, 8, 0),
                    Child = new TextBlock
                    {
                        Text = RegionVersLabel(voyage.Region),
                        FontSize = 10,
                        FontWeight = FontWeights.Bold
                    }
                });

                bandeau.Children.Add(new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#56B9E9")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 3, 8, 3),
                    Child = new TextBlock
                    {
                        Text = voyage.StyleVoyageAffiche,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    }
                });

                zoneImage.Children.Add(bandeau);

                var imageBorder = new Border
                {
                    CornerRadius = new CornerRadius(18, 18, 0, 0),
                    ClipToBounds = true,
                    Child = zoneImage
                };

                racine.Children.Add(imageBorder);

                var contenu = new StackPanel
                {
                    Margin = new Thickness(16)
                };

                contenu.Children.Add(new TextBlock
                {
                    Text = voyage.VillePays,
                    FontSize = 24,
                    FontWeight = FontWeights.Black,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D2733")),
                    TextWrapping = TextWrapping.Wrap
                });

                var ligneInfo = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                ligneInfo.Children.Add(new TextBlock
                {
                    Text = $"☀ {voyage.TemperatureMoyenne:0.#}°C",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F7A86")),
                    Margin = new Thickness(0, 0, 12, 0)
                });

                ligneInfo.Children.Add(new TextBlock
                {
                    Text = $"🗓 {voyage.NombreJours} jours",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F7A86"))
                });

                contenu.Children.Add(ligneInfo);

                var lignePrix = new Grid
                {
                    Margin = new Thickness(0, 14, 0, 0)
                };
                lignePrix.ColumnDefinitions.Add(new ColumnDefinition());
                lignePrix.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                lignePrix.Children.Add(new TextBlock
                {
                    Text = "Prix estimé",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F7A86")),
                    FontSize = 14
                });

                var prix = new TextBlock
                {
                    Text = $"{voyage.PrixTotal} €",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D98B9")),
                    FontSize = 30,
                    FontWeight = FontWeights.Black
                };
                Grid.SetColumn(prix, 1);
                lignePrix.Children.Add(prix);

                contenu.Children.Add(lignePrix);

                var infos = new WrapPanel
                {
                    Margin = new Thickness(0, 12, 0, 0)
                };

                infos.Children.Add(CreerTagInfo("✈ Vol"));
                infos.Children.Add(CreerTagInfo("🏨 Hôtel"));
                if (voyage.IdeesGratuites.Count > 0)
                    infos.Children.Add(CreerTagInfo("🆓 Gratuit"));

                contenu.Children.Add(infos);

                var btnVoir = new Button
                {
                    Content = "Explorer",
                    Height = 42,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(0, 16, 0, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#56B9E9")),
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.Transparent,
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = voyage
                };
                btnVoir.Click += BtnVoirVoyage_Click;

                contenu.Children.Add(btnVoir);

                racine.Children.Add(contenu);
                carte.Child = racine;
                PanelResultats.Children.Add(carte);
            }
        }

        private Border CreerTagInfo(string texte)
        {
            return new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2F7F9")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 8, 8),
                Child = new TextBlock
                {
                    Text = texte,
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F7A86"))
                }
            };
        }

        private async void BtnVoirVoyage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not PropositionVoyage voyage)
                return;

            _voyageActuel = voyage;
            await ChargerDetailAsync(voyage);
            AfficherDetail();
        }

        #endregion

        #region Détail

        private async System.Threading.Tasks.Task ChargerDetailAsync(PropositionVoyage voyage)
        {
            TxtVillePays.Text = voyage.VillePays;
            TxtRegionPays.Text = $"{voyage.Region} • {voyage.NombreJours} jours • {voyage.PrixTotal} €";
            TxtDetailRegionShort.Text = RegionVersLabel(voyage.Region);
            TxtDetailStyle.Text = voyage.StyleVoyageAffiche;
            TxtDescription.Text = string.IsNullOrWhiteSpace(voyage.Description)
                ? "Aucune description disponible."
                : voyage.Description;

            ChargerImage(ImgDestinationHero, voyage.UrlImage);

            TxtCompagnieSuggeree.Text = $"Compagnie suggérée : {voyage.CompagnieVol}";
            TxtLienVol.Text = "Rechercher sur Google Flights";
            TxtHotelSuggere.Text = voyage.NomHotel;
            TxtHotelLien.Text = "Hôtel conseillé selon ton style de voyage";

            TxtPrixVolLabel.Text = $"✈ Vol ({voyage.CompagnieVol})";
            TxtPrixVolValeur.Text = $"{voyage.PrixVol} €";
            TxtPrixHotelLabel.Text = $"🏨 Hôtel ({voyage.NomHotel})";
            TxtPrixHotelValeur.Text = $"{voyage.PrixHotel} €";
            TxtPrixActivitesLabel.Text = "🎟 Activités";
            TxtPrixActivitesValeur.Text = $"{voyage.PrixActivites} €";
            TxtPrixTotal.Text = $"{voyage.PrixTotal} €";

            var previsions = await _serviceMeteo.RecupererPrevisionsJournalieresAsync(
                voyage.Latitude,
                voyage.Longitude,
                7);
           

            ConstruireMeteo(previsions, voyage.TemperatureMoyenne);
            ConstruireProgramme(voyage.Itineraire);
            ConstruireListeTexte(PanelActivitesPayantes, voyage.Activites, "#56B9E9");
            ConstruireListeTexte(PanelIdeesGratuites, voyage.IdeesGratuites, "#32C48D");
        }

        private void ConstruireMeteo(List<PrevisionJournaliere> previsions, double temperatureSecours)
        {
            PanelPrevisionsMeteo.Children.Clear();

            if (previsions == null || previsions.Count == 0)
            {
                var cardErreur = new Border
                {
                    Width = 180,
                    Margin = new Thickness(0, 0, 14, 14),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7F9FB")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E5E2")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(16)
                };

                var stackErreur = new StackPanel();

                stackErreur.Children.Add(new TextBlock
                {
                    Text = "Prévisions indisponibles",
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D2733"))
                });

                stackErreur.Children.Add(new TextBlock
                {
                    Text = $"{temperatureSecours:0.#}°C",
                    Margin = new Thickness(0, 10, 0, 0),
                    FontSize = 22,
                    FontWeight = FontWeights.Black,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D98B9"))
                });

                cardErreur.Child = stackErreur;
                PanelPrevisionsMeteo.Children.Add(cardErreur);
                return;
            }

            foreach (var p in previsions.Take(7))
            {
                var card = new Border
                {
                    Width = 150,
                    Margin = new Thickness(0, 0, 14, 14),
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E5E2")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(14),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 12,
                        ShadowDepth = 2,
                        Opacity = 0.10,
                        Color = Colors.Black
                    }
                };

                var stack = new StackPanel();

                stack.Children.Add(new TextBlock
                {
                    Text = p.Jour.ToUpper(),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F7A86"))
                });

                stack.Children.Add(new TextBlock
                {
                    Text = IcôneMeteo(p.ResumeMeteo),
                    FontSize = 28,
                    Margin = new Thickness(0, 10, 0, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                stack.Children.Add(new TextBlock
                {
                    Text = $"{p.Temperature:0.#}°C",
                    FontSize = 20,
                    FontWeight = FontWeights.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D2733"))
                });

                stack.Children.Add(new TextBlock
                {
                    Text = p.ResumeMeteo,
                    Margin = new Thickness(0, 8, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F7A86")),
                    FontSize = 12
                });

                card.Child = stack;
                PanelPrevisionsMeteo.Children.Add(card);
            }
        }
        private void ConstruireProgramme(List<JourItineraire> itineraire)
        {
            PanelProgramme.Children.Clear();

            foreach (var jour in itineraire)
            {
                var bloc = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F7FA")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E5E2")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(18),
                    Margin = new Thickness(0, 0, 0, 12)
                };

                var stack = new StackPanel();

                stack.Children.Add(new TextBlock
                {
                    Text = jour.Titre,
                    FontWeight = FontWeights.Black,
                    FontSize = 15,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D98B9"))
                });

                if (!string.IsNullOrWhiteSpace(jour.DateLabel))
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = jour.DateLabel,
                        Margin = new Thickness(0, 4, 0, 8),
                        FontSize = 12,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F7A86"))
                    });
                }

                foreach (var etape in jour.Etapes)
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = etape,
                        Margin = new Thickness(0, 5, 0, 0),
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"))
                    });
                }

                bloc.Child = stack;
                PanelProgramme.Children.Add(bloc);
            }
        }

        private void ConstruireListeTexte(StackPanel panel, List<string> lignes, string couleurBord)
        {
            panel.Children.Clear();

            foreach (var ligne in lignes)
            {
                panel.Children.Add(new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(couleurBord)),
                    BorderThickness = new Thickness(4, 0, 0, 0),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 10),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 8,
                        ShadowDepth = 1,
                        Opacity = 0.06,
                        Color = Colors.Black
                    },
                    Child = new TextBlock
                    {
                        Text = ligne,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D2733"))
                    }
                });
            }
        }

        private static string IcôneMeteo(string resume)
        {
            if (string.IsNullOrWhiteSpace(resume))
                return "☀";

            resume = resume.ToLower();

            if (resume.Contains("orage")) return "⛈";
            if (resume.Contains("averse")) return "🌦";
            if (resume.Contains("pluie")) return "🌧";
            if (resume.Contains("neige")) return "❄";
            if (resume.Contains("brouillard")) return "🌫";
            if (resume.Contains("nuage")) return "☁";
            if (resume.Contains("partiellement")) return "⛅";
            if (resume.Contains("variable")) return "🌤";
            if (resume.Contains("ensole")) return "☀";
            if (resume.Contains("clair")) return "☀";

            return "☀";
        }

        private string RegionVersLabel(string region)
        {
            if (region.Contains("Asie", StringComparison.OrdinalIgnoreCase)) return "ASIA";
            if (region.Contains("Amérique", StringComparison.OrdinalIgnoreCase) || region.Contains("Caraïbes", StringComparison.OrdinalIgnoreCase)) return "AMERICAS";
            if (region.Contains("Europe", StringComparison.OrdinalIgnoreCase)) return "EUROPE";
            if (region.Contains("Afrique", StringComparison.OrdinalIgnoreCase)) return "AFRICA";
            if (region.Contains("Océanie", StringComparison.OrdinalIgnoreCase)) return "OCEANIA";
            if (region.Contains("Orient", StringComparison.OrdinalIgnoreCase)) return "MIDDLE EAST";
            return region.ToUpperInvariant();
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
    }
}