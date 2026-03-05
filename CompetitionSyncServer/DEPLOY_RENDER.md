# Deploiement Render (pas a pas)

Ce guide deploie le serveur `CompetitionSyncServer` sur Render en plan gratuit.

## 1) Preparer le depot GitHub

1. Mets ces fichiers dans ton repo GitHub:
- `CompetitionSyncServer/` (tout le dossier serveur)
- `render.yaml` (a la racine du repo)

2. Push sur `main`.

## 2) Creer le service sur Render

1. Va sur `https://dashboard.render.com/`.
2. Clique `New +`.
3. Clique `Blueprint`.
4. Connecte ton compte GitHub si besoin.
5. Selectionne ton repo `samdelerm/APP_RSK_ANDROID`.
6. Render detecte `render.yaml` et propose le service `competition-sync-server`.
7. Valide avec `Apply`.

Render va lancer le build Docker du dossier `CompetitionSyncServer` puis deploie automatiquement.

## 3) Recuperer l'URL publique

1. Ouvre le service `competition-sync-server` dans Render.
2. Copie l'URL du service, typiquement:
- `https://competition-sync-server.onrender.com`

3. Teste la sante du serveur dans ton navigateur:
- `https://competition-sync-server.onrender.com/api/health`

Tu dois voir un JSON avec `status: ok`.

## 4) Brancher l'app mobile

Dans l'app `MobileCompleteApp`:

1. Ouvre `Parametres`.
2. Dans `URL serveur de synchro`, mets:
- `https://competition-sync-server.onrender.com/`
3. Active `Activer la synchro auto`.
4. Sauvegarde.
5. Va dans `Videos` puis clique `Sync`.

## 5) Deploiement continu

Avec `autoDeploy: true` dans `render.yaml`:
- chaque push sur `main` redeploie automatiquement le serveur.

## Notes plan gratuit

- Le service peut se mettre en veille apres inactivite.
- La premiere requete apres veille peut etre plus lente (cold start).
- Les fichiers ecrits localement sur le conteneur (JSON) ne sont pas persistants a long terme.

Si tu veux une vraie persistence, ajoute une base (PostgreSQL Render) et je te fais la migration.
