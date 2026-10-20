# Expense Tracker frontend

React + TypeScript + Vite.

## Setup

Copy `.env.example` to `.env.local` and fill in:

- `VITE_API_URL`: the .NET API, e.g. `http://localhost:5000`.
- `VITE_SUPABASE_URL` and `VITE_SUPABASE_PUBLISHABLE_KEY`: from the Supabase project (Project Settings > API Keys). Both are public; never put a secret in a `VITE_` variable.

Users are created in the Supabase dashboard (sign-up is disabled).

## Commands

```sh
npm install
npm run dev     # http://localhost:5173
npm run lint
npm run build
```
