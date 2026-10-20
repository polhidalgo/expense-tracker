import { createClient } from "@supabase/supabase-js";

// The publishable key is public by design. It is safe because RLS blocks Supabase's Data API on our tables.
export const supabase = createClient(
  import.meta.env.VITE_SUPABASE_URL,
  import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY,
);
