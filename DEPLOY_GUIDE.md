# Deploy Guide — SpotifyPlaylistSampler

## Backend → Fly.io

L'unica cosa da ricordare è: backend prima, frontend dopo — perché ti serve l'URL di Fly per configurare Vercel.

```powershell
# 1. Installa Fly CLI
irm https://fly.io/install.ps1 | iex
fly auth signup

# 2. Dal folder backend/
cd backend
fly launch --no-deploy
fly volumes create data --region cdg --size 1

# 3. Configura secrets
fly secrets set `
  Spotify__ClientId="TUO_CLIENT_ID" `
  Spotify__ClientSecret="TUO_CLIENT_SECRET" `
  Spotify__RedirectUri="https://NOME-APP.fly.dev/api/auth/callback" `
  Jwt__Secret="STRINGA-RANDOM-ALMENO-32-CARATTERI" `
  FrontendUrl="https://NOME-APP.vercel.app" `
  Cors__AllowedOrigins__0="https://NOME-APP.vercel.app"

# 4. Deploy
fly deploy

# Comandi utili post-deploy:
# fly logs              → vedi i log
# fly status            → stato della macchina
# fly ssh console       → SSH nella VM
# fly secrets list      → lista secrets configurati
```

## Frontend → Vercel

1. Vai su https://vercel.com → "Add New Project" → importa il repo GitHub
2. **Root Directory:** `frontend`
3. **Build Command:** `ng build`
4. **Output Directory:** `dist/SpotifyPlaylistSampler/browser`
5. Clicca Deploy

> Prima di deployare, aggiorna l'URL del backend in:
> `frontend/src/environments/environment.prod.ts` → `apiUrl: "https://NOME-APP.fly.dev"`

## Checklist pre-deploy

- [ ] Redirect URI aggiunto su Spotify Dashboard ✅ (già fatto)
- [ ] `environment.prod.ts` aggiornato con URL backend reale
- [ ] Secrets configurati su Fly.io
- [ ] Backend deployed e raggiungibile
- [ ] Frontend deployed su Vercel
- [ ] Testare login end-to-end in produzione
