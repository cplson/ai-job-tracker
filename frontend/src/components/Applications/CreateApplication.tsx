import { useState } from "react";
import api from "../../services/api";
import SubmitButton from "../Common/SubmitButton";
import CancelButton from "../Common/CancelButton";

interface CreateApplicationForm {
  company: string;
  jobTitle: string;
  jobDescription: string;
  status: string;
}

export default function CreateApplication() {
  const [form, setForm] = useState<CreateApplicationForm>({
    company: "",
    jobTitle: "",
    jobDescription: "",
    status: "Draft",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async () => {
    setError("");
    setLoading(true);
    try {
      await api.post("/applications", form);
      // navigate with success handled by SubmitButton
    } catch (err) {
      console.error(err);
      setError("Failed to create application");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="col-lg-8">
      <h2>Create New Application</h2>
      {error && <div className="alert alert-danger">{error}</div>}
      <form  
          onSubmit={async (e) => {
        e.preventDefault();
        await handleSubmit();
      }}>
        <div className="mb-3">
          <label className="form-label">Company</label>
          <input
            className="form-control"
            value={form.company}
            onChange={(e) => setForm({ ...form, company: e.target.value })}
          />
        </div>

        <div className="mb-3">
          <label className="form-label">Job Title</label>
          <input
            className="form-control"
            value={form.jobTitle}
            onChange={(e) => setForm({ ...form, jobTitle: e.target.value })}
          />
        </div>

        <div className="mb-3">
          <label className="form-label">Job Description</label>
          <textarea
            className="form-control"
            value={form.jobDescription}
            onChange={(e) =>
              setForm({ ...form, jobDescription: e.target.value })
            }
          />
        </div>

        <div className="d-flex gap-2">
          <SubmitButton
            label="Create Application"
            isLoading={loading}
            fallbackPath="/applications"
            successState="created"
            type="submit"
            onClick={handleSubmit}
          />

          <CancelButton fallbackPath="/applications" />
        </div>
      </form>
    </div>
  );
}