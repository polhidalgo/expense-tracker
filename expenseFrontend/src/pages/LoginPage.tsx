import { useState, type FormEvent } from "react";
import { supabase } from "../supabase";

function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [signingIn, setSigningIn] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSigningIn(true);
    setError("");
    const { error } = await supabase.auth.signInWithPassword({ email, password });
    if (error) setError(error.message);
    setSigningIn(false);
  }

  return (
    <form className="expense-form" onSubmit={handleSubmit}>
      <h2>Sign in</h2>

      <label>
        Email
        <input type="email" required autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
      </label>

      <label>
        Password
        <input
          type="password"
          required
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
      </label>

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}

      <div className="actions">
        <button type="submit" disabled={signingIn}>
          {signingIn ? "Signing in..." : "Sign in"}
        </button>
      </div>
    </form>
  );
}

export default LoginPage;
