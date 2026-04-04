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
  const [aiResult, setAiResult] = useState<any>(null);
  const [aiLoading, setAiLoading] = useState(false);
  const [aiError, setAiError] = useState('');

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

  const handleAnalyze = async () => {
    setAiLoading(true);
    setAiError('');

    try {
      const res = await api.post(`/ai/analyze/${application?.id}`);
      setAiResult(res.data);
    } catch (err) {
      console.error(err);
      setAiError("Failed to analyze application");
    } finally {
      setAiLoading(false);
    }
  };

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
        
        {/* AI SECTION */}
        <div className="card shadow">
          <div className="card-body">
            <h5>AI Insights</h5>

            {!aiResult && (
              <button
                className="btn btn-primary mb-3"
                onClick={handleAnalyze}
                disabled={aiLoading}
              >
                {aiLoading ? "Analyzing..." : "Analyze Application"}
              </button>
            )}

            {aiError && <div className="alert alert-danger">{aiError}</div>}

            {aiResult && (
              <>
                <h6>Match Score</h6>
                <div className="mb-3">
                  <span className="badge bg-success fs-6">
                    {aiResult.score}%
                  </span>
                </div>

                <h6>Suggestions</h6>
                <ul>
                  {aiResult.suggestions.map((s: string, i: number) => (
                    <li key={i}>{s}</li>
                  ))}
                </ul>

                <h6>Missing Keywords</h6>
                <ul>
                  {aiResult.missingKeywords.map((k: string, i: number) => (
                    <li key={i}>{k}</li>
                  ))}
                </ul>
              </>
            )}
          </div>
        </div>
    </div>
  );
}