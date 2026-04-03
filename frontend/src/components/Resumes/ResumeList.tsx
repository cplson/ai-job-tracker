import { useEffect, useState } from "react";
import api from "../../services/api";
import DeleteButton from "../Common/DeleteButton";
import { useLocation, useNavigate, Link } from "react-router-dom";

interface ResumeDto {
  id: string;
  fileName: string;
  uploadedAt: string;
}

export default function ResumeList() {
  const [resumes, setResumes] = useState<ResumeDto[]>([]);
  const [error, setError] = useState("");
  const location = useLocation()
  const navigate = useNavigate()
  const [showSuccess, setShowSuccess] = useState<string | null>(null);

  useEffect(() => {
    const success = location.state?.success;
    if (success === "created") setShowSuccess("Resume uploaded successfully");
    else if (success === "deleted") setShowSuccess("Resume deleted successfully");

    navigate(location.pathname, { replace: true, state: {} });

    async function fetchResumes() {
      try {
        const res = await api.get<ResumeDto[]>("/resumes/me");
        setResumes(res.data);
      } catch (err) {
        console.error(err);
        setError("Failed to load resumes");
      }
    }

    fetchResumes();
  }, []);

  const handleDownload = async (id: string, fileName: string) => {
    try {
      const res = await api.get(`/resumes/${id}/download`, {
        responseType: "blob",
      });

      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement("a");

      link.href = url;
      link.setAttribute("download", fileName);
      document.body.appendChild(link);
      link.click();

      link.remove();
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <div className="col-lg-8">
          {showSuccess && (
            <div className="alert alert-success alert-dismissible fade show" role="alert">
              {showSuccess}
              <button
                type="button"
                className="btn-close"
                onClick={() => setShowSuccess(null)}
              ></button>
            </div>
          )}
      <div className="d-flex justify-content-between mb-3">
        <h2>My Resumes</h2>
        <Link to="/resumes/upload" className="btn btn-primary">
          + Upload Resume
        </Link>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card">
        <div className="card-body">
          {resumes.length === 0 ? (
            <p>No resumes uploaded yet.</p>
          ) : (
            <table className="table table-striped">
              <thead>
                <tr>
                  <th>File Name</th>
                  <th>Uploaded</th>
                  <th></th>
                  {/* <th></th> */}
                </tr>
              </thead>
              <tbody>
                {resumes.map((r) => (
                  <tr key={r.id} className="align-middle">
                    <td>📄 {r.fileName}</td>
                    <td>{new Date(r.uploadedAt).toLocaleString()}</td>
                    <td className="d-flex gap-2">
                      <button
                        className="btn btn-outline-primary btn-sm"
                        onClick={() => handleDownload(r.id, r.fileName)}
                      >
                        Download
                      </button>
                      <DeleteButton
                        label="Delete"
                        fallbackPath="/resumes"
                        successState="deleted"
                        onDelete={async () => {
                          try{
                            await api.delete(`/resumes/${r.id}`)
                            setShowSuccess("Resume deleted successfully")
                            setResumes(prev => prev.filter(resume => resume.id !== r.id));
                          } catch (err: any){
                            console.log(err)
                          }
                        }}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}