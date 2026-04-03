import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import api from '../../services/api';
import type { ApplicationDto, ResumeDto } from '../../types';
import BackButton from '../Common/BackButton';
import DeleteButton from '../Common/DeleteButton';
import EditButton from '../Common/EditButton';

export default function ApplicationDetails() {
  const { id } = useParams();
  const [application, setApplication] = useState<ApplicationDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [resume, setResume] = useState<ResumeDto | null>(null);

  // TO DO - LOW PRIORITY: update backend get application to return the 
  //                resume with it, eliminating the need for an extra api request
  useEffect(() => {
    async function fetchApplication() {
      try {
        const appRes = await api.get<ApplicationDto>(`/applications/${id}`);
        setApplication(appRes.data);

        if (appRes.data.resumeId) {
          const res = await api.get<ResumeDto>(
            `/resumes/${appRes.data.resumeId}`
          );
          setResume(res.data);
        }
      } catch (err) {
        console.error(err);
        setError('Failed to load application');
      } finally {
        setLoading(false);
      }

    }
    fetchApplication();
  }, [id]);

  if (loading) return <p>Loading...</p>;
  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!application) return <p>Application not found</p>;

  return (
    <div className="row">
        <div className="col-lg-8">
            <div className="card shadow mb-4">
                <div className="d-flex justify-content-end gap-2 m-3">
                <EditButton to={`/applications/${application.id}/edit`} />
                <DeleteButton
                    fallbackPath="/applications"
                    successState="deleted"
                    onDelete={async () => {
                    await api.delete(`/applications/${application.id}`);
                    }}
                />
            </div>
            <div className="card-body">
                <h3 className="mb-3">{application.jobTitle}</h3>
                <h5 className="text-muted mb-3">{application.company}</h5>

                <span className="badge bg-primary mb-3">
                {application.status}
                </span>

                <hr />

                <h5>Job Description</h5>
                <p style={{ whiteSpace: 'pre-wrap' }}>
                {application.jobDescription || 'No description provided.'}
                </p>
                {resume && (
                  <div className="mb-3">
                    <h5>Resume</h5>
                    <a 
                      href={resume.url} 
                      target="_blank" 
                      rel="noopener noreferrer"
                      className="btn btn-outline-secondary"
                    >
                      Download / View Resume
                    </a>
                  </div>
                )}
              </div>
            </div>
            <BackButton label="Back to Applications" fallbackPath="/applications" ignoreHistory />
        </div>

        {/* 🔥 Future AI section */}
        <div className="col-lg-4">
            <div className="card shadow">
            <div className="card-body">
                <h5>AI Insights (Coming Soon)</h5>
                <p className="text-muted">
                Resume match score, keyword analysis, and suggestions will appear here.
                </p>
            </div>
            </div>
        </div>
    </div>
  );
}