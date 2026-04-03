import { useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../services/api";
import SubmitButton from "../Common/SubmitButton";
import CancelButton from "../Common/CancelButton";

export default function UploadResume() {
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const navigate = useNavigate();

  const handleSubmit = async () => {
    if (!file) {
      setError("Please select a file.");
      return;
    }

    setError("");
    setLoading(true);

    try {
      const formData = new FormData();
      formData.append("file", file);

      await api.post("/resumes", formData, {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      });

      navigate("/resumes", { state: { success: "created" } });
    } catch (err) {
      console.error(err);
      setError("Failed to upload resume");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="col-lg-6">
      <h2>Upload Resume</h2>

      {error && <div className="alert alert-danger">{error}</div>}

      <form
        onSubmit={async (e) => {
          e.preventDefault();
          await handleSubmit();
        }}
      >
        <div className="mb-3">
          <label className="form-label">Resume File</label>
          <input
            type="file"
            className="form-control"
            onChange={(e) => setFile(e.target.files?.[0] || null)}
          />
        </div>

        <div className="d-flex gap-2">
          <SubmitButton label="Upload Resume" isLoading={loading} />
          <CancelButton fallbackPath="/resumes" />
        </div>
      </form>
    </div>
  );
}