import { useEffect, useState } from 'react';
import api from '../../services/api';
import type { ResumeDto } from '../../types';

export default function ResumeList() {
  const [resumes, setResumes] = useState<ResumeDto[]>([]);

  useEffect(() => {
    async function fetchResumes() {
      try {
        const res = await api.get<ResumeDto[]>('/resumes/me');
        setResumes(res.data);
      } catch (err) {
        console.error(err);
      }
    }
    fetchResumes();
  }, []);

  return (
    <div>
      <h2 className="mb-4">My Resumes</h2>

      <div className="card">
        <div className="card-body">
          {resumes.length === 0 ? (
            <p>No resumes uploaded.</p>
          ) : (
            <ul className="list-group">
              {resumes.map(r => (
                <li key={r.id} className="list-group-item d-flex justify-content-between">
                  <span>{r.fileName}</span>
                  <small className="text-muted">
                    {new Date(r.uploadedAt).toLocaleString()}
                  </small>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}