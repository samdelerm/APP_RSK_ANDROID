# CompetitionSyncServer

API ASP.NET Core pour synchroniser les videos de matchs de `competition.robot-soccer-kit.com` entre plusieurs clients mobiles.

## Endpoints
- `GET /api/health`
- `GET /api/competitions`
- `GET /api/videos`
- `POST /api/videos`
- `DELETE /api/videos/{id}`

## Lancer en local
```powershell
dotnet run --project CompetitionSyncServer.csproj
```

Par defaut: `http://localhost:5267` (ou le port affiche par `dotnet run`).

## Stockage
Le serveur persiste les videos en JSON:
- Dev: `App_Data/videos.dev.json`
- Prod: `App_Data/videos.json` (ou `Storage:VideoFilePath`)

## Integration MobileCompleteApp
Dans l'app mobile, ouvre `Parametres` et renseigne:
- `URL serveur de synchro`: ex `http://localhost:5267/`
- `Activer la synchro auto`: active

Puis va dans `Videos`:
- `Sync` pour recuperer les videos serveur
- `Publier` pour envoyer une nouvelle video au serveur

## GitHub Actions
Workflow present dans `.github/workflows/ci.yml`:
- restore
- build
- publish
- upload artifact

Tu peux ensuite deployer cet artifact sur ton hebergeur cible.
