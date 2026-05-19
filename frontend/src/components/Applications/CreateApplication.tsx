import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../services/api";
import SubmitButton from "../Common/SubmitButton";
import CancelButton from "../Common/CancelButton";
import { ResumeDto, CreateApplicationForm } from "../../types";
import { getApiErrorMessage } from "../../utils/apiErrors";

export default function CreateApplication() {

  const [form, setForm] = useState<CreateApplicationForm>({
    company: "",
    jobTitle: "",
    jobDescription: "",
    status: "Draft",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const navigate = useNavigate();
  const [resumes, setResumes] = useState<ResumeDto[]>([]);

  useEffect(() => {
  async function fetchResumes() {
    try {
      const res = await api.get<ResumeDto[]>('/resumes/me');
      setResumes(res.data);
    } catch (err) {
      console.error("Failed to fetch resumes", err);
    }
  }
  fetchResumes();
}, []);

  const handleSubmit = async () => {
    setError("");

    if (!form.company.trim() || !form.jobTitle.trim()) {
      setError("Company and job title are required.");
      return;
    }

    setLoading(true);
    try {
      const payload = {
        company: form.company.trim(),
        jobTitle: form.jobTitle.trim(),
        jobDescription: form.jobDescription.trim(),
        status: form.status,
        ...(form.resumeId ? { resumeId: form.resumeId } : {}),
      };
      await api.post("/applications", payload);
      navigate("/applications", { state: { success: "created" } });
    } catch (err) {
      console.error(err);
      setError(getApiErrorMessage(err, "Failed to create application"));
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
            maxLength={2000}
            onChange={(e) =>
              setForm({ ...form, jobDescription: e.target.value })
            }
          />
        </div>

        <div className="mb-3">
          <label className="form-label">Link Resume</label>
          <select
            className="form-select"
            value={form.resumeId || ""}
            onChange={(e) =>
              setForm({
                ...form,
                resumeId: e.target.value || undefined,
              })
            }
          >
            <option value="">-- None --</option>
            {resumes.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
              </option>
            ))}
          </select>
        </div>

        <div className="d-flex gap-2">
          <SubmitButton
            label="Create Application"
            isLoading={loading}
          />

          <CancelButton fallbackPath="/applications" />
        </div>
      </form>
    </div>
  );
}