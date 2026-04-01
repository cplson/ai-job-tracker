import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Login from './components/Auth/Login';
import ApplicationList from './components/Applications/ApplicationList';
import ResumeList from './components/Resumes/ResumeList';
import Navbar from './components/Layout/Navbar';

const ProtectedRoute = ({ children }: { children: React.JSX.Element }) => {
  const token = localStorage.getItem('jwt');
    return token ? (
    <>
      <Navbar />
      <div className="container mt-4">{children}</div>
    </>
  ) : (
    <Navigate to="/" replace />
  );
};

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Login />} />
        <Route path="/applications" element={<ProtectedRoute><ApplicationList /></ProtectedRoute>} />
        <Route path="/resumes" element={<ProtectedRoute><ResumeList /></ProtectedRoute>} />
      </Routes>
    </Router>
  );
}
export default App
