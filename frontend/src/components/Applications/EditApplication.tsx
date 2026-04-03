import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import api from "../../services/api";
import SubmitButton from "../Common/SubmitButton";
import CancelButton from "../Common/CancelButton";
import { ApplicationDto, ResumeDto } from "../../types";

export default function EditApplication() {
  const { id } = useParams();
  const [form, setForm] = useState<ApplicationDto>({
    id: "",
    company: "",
    jobTitle: "",
    jobDescription: "",
    status: "Draft",
    resumeId: "",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const navigate = useNavigate();
  const [resumes, setResumes] = useState<ResumeDto[]>([]);

  useEffect(() => {
    async function fetchApplication() {
      try {
        const res = await api.get(`/applications/${id}`);

        setForm({
          id: res.data.id,
          company: res.data.company,
          jobTitle: res.data.jobTitle,
          jobDescription: res.data.jobDescription,
          status: res.data.status,
          resumeId: res.data.resumeId,
        });
      } catch (err) {
        console.error(err);
        setError("Failed to load application");
      } finally {
        setLoading(false)
      }
    }

    async function fetchResumes() {
      try {
        const res = await api.get<ResumeDto[]>('/resumes/me');
        setResumes(res.data);
      } catch (err) {
        console.error("Failed to fetch resumes", err);
      }
    }

    if (id){
      fetchApplication();
      fetchResumes();
    } 
  }, [id]);

  const handleSubmit = async (e: React.SubmitEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      await api.put(`/applications/${id}`, form);
      navigate("/applications", {state: { success: "updated" }})
    } catch (err) {
      console.error(err);
      setError("Failed to update application");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="col-lg-8">
      <h2>Edit Application</h2>
      {error && <div className="alert alert-danger">{error}</div>}
      <form onSubmit={handleSubmit}>
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

        <div className="mb-3">
          <label className="form-label">Link Resume</label>
          <select
            className="form-select"
            value={form.resumeId || ""}
            onChange={(e) => setForm({ ...form, resumeId: e.target.value })}
          >
            <option value="">-- None --</option>
            {resumes.map((r) => (
              <option key={r.id} value={r.id}>
                {r.fileName}
              </option>
            ))}
          </select>
        </div>

        <div className="d-flex gap-2">
          <SubmitButton
            label="Update"
            isLoading={loading}
            />

          <CancelButton label="Back" fallbackPath={`/applications/${id}`} />
        </div>
      </form>
    </div>
  );
}