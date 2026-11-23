# CONSAN - Middle Euphrates Workshop

Full-stack ASP.NET Core 10 web app + admin dashboard, packaged in Docker with SQL Server 2022.

---

## Quick Start (5 minutes)

### 1. Prerequisites

- Docker Desktop - https://www.docker.com/products/docker-desktop/
- PowerShell 7+ (optional) - https://aka.ms/powershell
- SQL Server Management Studio (optional) - https://aka.ms/ssms

### 2. Clone the Repository

    git clone <repository-url>
    cd text_2

### 3. Create .env File

    Copy-Item .env.example .env
    notepad .env

Fill in real values.

### 4. Start Everything

    docker compose up -d --build

Wait ~2 minutes.

### 5. Access the App

| URL | Purpose |
|---|---|
| http://localhost:8080/ | Public website |
| http://localhost:8080/admin | Admin dashboard |
| http://localhost:8080/industrial-services | Industrial services |
| http://localhost:8080/explorer | Explorer |
| http://localhost:8080/guide | Comprehensive guide |

---

## Admin Dashboard

- URL: http://localhost:8080/admin
- Password: value of ADMIN_PASSWORD in .env

---

## Database Access (SSMS)

| Setting | Value |
|---|---|
| Server name | localhost,1433 |
| Authentication | SQL Server Authentication |
| Login | sa |
| Password | value of MSSQL_SA_PASSWORD in .env |
| Database | ConsanDb |
| Encrypt connection | Optional |
| Trust server certificate | Yes |

---

## Backup

Automatic: Daily at 2:00 AM. Location: ./backups/. Retention: 7 days.

Manual:

    powershell -ExecutionPolicy Bypass -File .\scripts\backup-db.ps1

---

## Common Commands

    docker compose up -d
    docker compose down
    docker compose up -d --build consan_app
    docker compose logs -f consan_app
    docker compose logs -f consan_sqlserver
    docker ps

---

## Languages

The public site is in five languages, in this order: Arabic (default), Kurdish (Sorani), Persian, Turkish and English.
Arabic, Kurdish and Persian are shown right to left; Turkish and English left to right.

- On a visitor's first visit, a language chooser opens before the site; the choice is remembered in the browser.

- The language switcher is in the header (`Site/Shared.cs`); the choice is remembered in the browser.
- Built-in text: Arabic/English/Kurdish in `Site/Defaults.cs` and `Program.cs`, Persian/Turkish in `Site/LangFaTr.cs`.
- Everything is editable in the dashboard: **محتوى الصفحة الرئيسية** and **الخدمات الصناعية** have a tab per language, and every map pin, archive photo, gallery item, explorer card and guide entry has Persian and Turkish fields.
- If a text has no Persian version yet, the Arabic one is shown; for Turkish, the English one.
- On first start after this update, a server with saved content gets the built-in Persian/Turkish text added to empty fields only. Nothing already written is changed.

---

## Orion — the AI assistant

Orion (أوريون) is the workshop's AI assistant on every public page (Google Gemini, model `gemini-3.1-flash-lite`, falling back to `gemini-3.5-flash-lite` when it is over quota or busy).

- Right after a visitor chooses a language (and once per visit after that), Orion greets them in that language with three buttons: book a visit, WhatsApp, or ask Orion.
- Orion is patient and guiding: it answers only from the information the owner provides, never invents prices or confirms times, replies in the visitor's language (detected from what they type), and leads them to a booking request, a call or a WhatsApp message.
- **Bookings:** Orion collects name, phone, service, vehicle, problem and preferred time, repeats them for the visitor to confirm, and saves a booking request (`data/bookings.json`). The visitor then sees a card with "send on WhatsApp" (a ready message to Abu Mustafa) and "call". The workshop calls to confirm the time. Real calendar scheduling can be added later, once that account is set up.
- The Gemini API key stays on the server (`GEMINI_API_KEY`). The browser only talks to `/api/chat` on this site.
- Without a key, the chat button does not appear.

### Setup

1. Create a key at https://aistudio.google.com/apikey (Google account → **Create API key**).
2. Add it to the server's environment. Never put it in a file that is committed:
   - Local Docker: add `GEMINI_API_KEY=...` to `.env` (git ignores it).
   - Railway: project → service `consan-project` → **Variables** → **New Variable** → `GEMINI_API_KEY`.
3. Open http://localhost:8080/admin → **أوريون (المساعد الذكي)**. New booking requests are listed at the top (call the customer, then set the status). Fill in the three boxes:
   - **الشخصية (Personality):** how the assistant talks.
   - **الأسئلة والأجوبة (Q&A):** the only facts it may use. Starter text from the site is filled in; check it, especially the opening hours, and add the workshop's own process details (there is a marked place for them).
   - **عند عدم معرفة الجواب (Fallback):** what to do when the answer is unknown, for example give the phone number or take a name and number for a callback.
4. Press **حفظ التعليمات**. **عرض التعليمات الكاملة** shows the full prompt the assistant receives (the three boxes plus the fixed rules).

### Cost controls

| Variable | Default | Meaning |
|---|---|---|
| `GEMINI_MODEL` | `gemini-3.1-flash-lite` | Model name (Google no longer offers the 2.5 models to new keys) |
| `GEMINI_FALLBACK_MODEL` | `gemini-3.5-flash-lite` | Used when the main model is over quota or busy; `none` turns it off |
| `CHAT_PER_MINUTE` | 6 | Messages per visitor IP per minute |
| `CHAT_PER_DAY` | 40 | Messages per visitor IP per day |
| `CHAT_GLOBAL_PER_DAY` | 1500 | Messages for the whole site per day (hard cap) |
| `CHAT_MAX_CHARS` | 500 | Longest visitor message |
| `CHAT_HISTORY` | 8 | Earlier messages sent with each question |
| `CHAT_MAX_OUTPUT_TOKENS` | 350 | Longest reply |
| `CHAT_LOG_DAYS` | 180 | Days of conversation logs kept |

Limits are kept in memory, so they reset when the app restarts.

On Google's free tier the key allows only a few requests per minute per model, and Google may use free-tier conversations to improve its products. For real traffic, turn on billing for the key's project in Google AI Studio.

### Conversation logs

Every message and reply is saved to `data/chat-logs/YYYY-MM-DD.jsonl` (on Railway: the `/app/data` volume, which deploys never touch; included in `scripts/backup-railway-data.sh` backups of `/app/data`).
Review them in the dashboard under **المساعد الذكي → سجل المحادثات**: conversations by day, a filter for conversations where the visitor left a phone number (to call them back), and a download of the day's file.
Visitor IPs are stored masked (last part replaced with `x`).

### Deploy

1. Set `GEMINI_API_KEY` in Railway Variables (see Setup).
2. Merge the branch into `master` and push. GitHub Actions deploys to Railway automatically.
3. Open https://kormanyworkshop.com/admin → **أوريون (المساعد الذكي)**, check the status shows the key as set, review the three boxes and save.
4. Open the site, press the chat button and ask a question in each language.

---

## Troubleshooting

App won't start:

    docker compose logs --tail=100 consan_app

SQL Server not healthy:

    docker compose logs --tail=100 consan_sqlserver
    docker inspect --format='{{.State.Health.Status}}' consan_sqlserver

Port 8080 in use: edit docker-compose.yml and change 8080:8080 to 8090:8080.

Reset everything (destroys data!):

    docker compose down -v
    docker compose up -d --build

---

## Health Check

    powershell -ExecutionPolicy Bypass -File .\check-health.ps1

---

Copyright 2026 CONSAN. All rights reserved.
