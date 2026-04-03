import { useEffect, useState } from "react";
import api from "../../services/api";
import DeleteButton from "../Common/DeleteButton";

interface ResumeDto {
  id: string;
  fileName: string;
  uploadedAt: string;
}

export default function ResumeList() {
  const [resumes, setResumes] = useState<ResumeDto[]>([]);
  const [error, setError] = useState("");

  useEffect(() => {
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

  return (
    <div className="col-lg-8">
      <h2>My Resumes</h2>

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
                </tr>
              </thead>
              <tbody>
                {resumes.map((r) => (
                  <tr key={r.id}>
                    <td>{r.fileName}</td>
                    <td>{new Date(r.uploadedAt).toLocaleString()}</td>
                    <td>
                      <DeleteButton
                        label="Delete"
                        fallbackPath="/resumes"
                        successState="deleted"
                        onDelete={async () => {
                          await api.delete(`/delete/${r.id}`)
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