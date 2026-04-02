import { useState, useEffect } from "react";
import { useParams } from "react-router-dom";
import api from "../../services/api";
import SubmitButton from "../Common/SubmitButton";

interface UpdateApplicationForm {
  company: string;
  jobTitle: string;
  jobDescription: string;
  status: string;
}

export default function EditApplication() {
  const { id } = useParams();
  const [form, setForm] = useState<UpdateApplicationForm>({
    company: "",
    jobTitle: "",
    jobDescription: "",
    status: "Draft",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  // Load existing application
  useEffect(() => {
    async function fetchApplication() {
      try {
        const res = await api.get(`/applications/${id}`);
        setForm({
          company: res.data.company,
          jobTitle: res.data.jobTitle,
          jobDescription: res.data.jobDescription,
          status: res.data.status,
        });
      } catch (err) {
        console.error(err);
        setError("Failed to load application");
      }
    }

    if (id) fetchApplication();
  }, [id]);

  const handleSubmit = async () => {
    setError("");
    setLoading(true);
    try {
      await api.put(`/applications/${id}`, form);
      // navigate with success handled by SubmitButton
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
      <form>
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

        <SubmitButton
          label="Update Application"
          isLoading={loading}
          fallbackPath="/applications"
          successState="updated"
          onClick={handleSubmit}
        />
      </form>
    </div>
  );
}