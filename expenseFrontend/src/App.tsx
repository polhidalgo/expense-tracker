
import { useEffect, useState } from "react";
import { getApiHealth } from "./lib/api";

function App() {
  const [status, setStatus] = useState("Checking...");

  useEffect(() => {
    getApiHealth()
      .then((data) => setStatus(data.status))
      .catch(() => setStatus("Disconnected"));
  }, []);

  return (
    <main style={{ padding: "2rem" }}>
      <h1>Expense Tracker</h1>
      <p>Backend status: {status}</p>
    </main>
  );
}

export default App;
