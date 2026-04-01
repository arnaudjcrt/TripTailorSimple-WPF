<h1 align="center">TripTailor</h1>

<p align="center">
  Application intelligente de planification de voyages basée sur des API temps réel.
</p>

<p align="center">
  <strong>Recherche • Analyse • Recommandation • Personnalisation</strong>
</p>

<hr/>

<h2>Présentation</h2>

<p>
TripTailor est une application desktop développée en WPF (C#) permettant de générer des propositions de voyages personnalisées en fonction de critères utilisateurs :
budget, climat, durée, région et style de voyage.
</p>

<p>
L’application exploite plusieurs API externes afin de fournir des résultats dynamiques, réalistes et exploitables immédiatement :
météo, données géographiques, contenus enrichis et suggestions d’activités.
</p>

<hr/>

<h2>Fonctionnalités principales</h2>

<ul>
  <li>Recherche de destinations intelligente (multi-critères)</li>
  <li>Suggestions personnalisées basées sur le budget et le style</li>
  <li>Affichage météo sur 7 jours avec interprétation visuelle</li>
  <li>Génération automatique d’itinéraires jour par jour</li>
  <li>Recommandations d’activités payantes et gratuites</li>
  <li>Estimation complète des coûts (vol, hôtel, activités)</li>
  <li>Recherche libre via API externe (ville, pays, destination)</li>
  <li>Interface moderne et dynamique (WPF)</li>
</ul>

<hr/>

<h2>Architecture du projet</h2>

<pre>
TripTailorSimple.WPF/
│
├── Models/                 → Structures de données (Voyage, Critères, etc.)
├── Services/               → Logique métier + appels API
│   ├── ServiceMeteo
│   ├── ServiceWikipedia
│   ├── ServicePays
│   ├── ServiceRechercheVoyage
│   ├── ServiceRechercheLibre
│   └── ServiceSuggestionsVoyage
│
├── Views/                  → Interface utilisateur (XAML + code-behind)
│   ├── MainWindow.xaml
│   └── ChatWindow.xaml
│
└── Resources/              → Assets visuels
</pre>

<hr/>

<h2>Technologies utilisées</h2>

<ul>
  <li>C# / .NET</li>
  <li>WPF (Windows Presentation Foundation)</li>
  <li>Architecture MVVM simplifiée</li>
  <li>HTTP Client pour appels API</li>
  <li>JSON (traitement des données externes)</li>
</ul>

<hr/>

<h2>APIs intégrées</h2>

<ul>
  <li>API météo (prévisions sur 7 jours)</li>
  <li>API Wikipedia (descriptions et images)</li>
  <li>API pays (drapeaux)</li>
  <li>API personnalisée de recherche libre</li>
</ul>

<hr/>

<h2>Logique intelligente</h2>

<p>
Le système repose sur un moteur de scoring permettant de classer les destinations selon leur pertinence :
</p>

<ul>
  <li>Respect du budget</li>
  <li>Correspondance climatique</li>
  <li>Compatibilité régionale</li>
  <li>Nombre d’activités disponibles</li>
</ul>

<p>
Chaque destination reçoit un score, puis est triée afin de proposer les meilleures options à l’utilisateur.
</p>

<hr/>

<h2>Interface utilisateur</h2>

<p>
L’interface a été conçue pour offrir une expérience fluide et intuitive :
</p>

<ul>
  <li>Cartes dynamiques pour les résultats</li>
  <li>Navigation multi-sections (Accueil, Résultats, Détail)</li>
  <li>Affichage visuel des données météo</li>
  <li>Design moderne basé sur des composants (Border, StackPanel)</li>
</ul>

<hr/>

<h2>Objectifs du projet</h2>

<ul>
  <li>Créer une application complète intégrant plusieurs APIs</li>
  <li>Mettre en pratique une architecture propre en WPF</li>
  <li>Développer un système de recommandation intelligent</li>
  <li>Améliorer l’expérience utilisateur sur desktop</li>
</ul>

<hr/>

<h2>Installation</h2>

<pre>
git clone https://github.com/ton-repo/triptailor.git
cd triptailor
dotnet build
dotnet run
</pre>

<hr/>

<h2>Perspectives d’évolution</h2>

<ul>
  <li>Ajout d’un système de comptes utilisateurs</li>
  <li>Historique des recherches</li>
  <li>Connexion à des APIs de réservation réelles</li>
  <li>Version mobile (MAUI ou Flutter)</li>
</ul>

<hr/>

<h2>Auteur</h2>

<p>
Amine Serhouani<br/>
Arnaud Jancart <br/>
Étudiant en BTS CIEL
</p>

<hr/>

<p align="center">
  Projet académique – 2026
</p>
