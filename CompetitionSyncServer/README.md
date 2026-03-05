# CompetitionSyncServer

API ASP.NET Core pour synchroniser les videos de matchs de `competition.robot-soccer-kit.com` entre plusieurs clients mobiles.

## Endpoints
- `GET /` (page admin stylisee pour envoyer des notifications)
- `GET /api/health`
- `GET /api/competitions`
- `GET /api/videos`
- `POST /api/videos`
- `DELETE /api/videos/{id}`
- `POST /api/videos/{id}/comments`
- `GET /api/notifications?sinceUtc=...`
- `POST /api/notifications/broadcast`

## Lancer en local
```powershell
dotnet run --project CompetitionSyncServer.csproj
```

Par defaut: `http://localhost:5267` (ou le port affiche par `dotnet run`).

## Stockage
Le serveur persiste les videos en JSON:
- Dev: `App_Data/videos.dev.json`
- Prod: `App_Data/videos.json` (ou `Storage:VideoFilePath`)

Notifications serveur:
- Dev: `App_Data/notifications.dev.json`
- Prod: `App_Data/notifications.json` (ou `Storage:NotificationsFilePath`)

## Integration MobileCompleteApp
L'app mobile utilise automatiquement:
- `https://competition-sync-server.onrender.com`

Dans `Parametres`:
- `Activer la synchro auto`: active

Puis va dans `Videos`:
- `Sync` pour recuperer les videos serveur
- `Publier` pour envoyer une nouvelle video au serveur
- `Com` pour commenter une video

## Envoyer une notification aux utilisateurs
Option 1 (recommande): ouvre `https://competition-sync-server.onrender.com/` et utilise le formulaire admin.

Option 2 (API):

```powershell
curl -X POST "https://competition-sync-server.onrender.com/api/notifications/broadcast" \
	-H "Content-Type: application/json" \
	-d '{"title":"Info competition","message":"Nouveau planning disponible."}'
```

Si `Notifications:AdminKey` est defini sur le serveur, ajoute:

```powershell
-H "X-Admin-Key: <ta-cle>"
```

La page admin `/` exige aussi ce meme code dans le champ `Code admin`.

## GitHub Actions
Workflow present dans `.github/workflows/ci.yml`:
- restore
- build
- publish
- upload artifact

Tu peux ensuite deployer cet artifact sur ton hebergeur cible.
