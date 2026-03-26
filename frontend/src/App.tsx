import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Login from './components/Auth/Login';
import ApplicationList from './components/Applications/ApplicationList';
import ResumeList from './components/Resumes/ResumeList';

const ProtectedRoute = ({ children }: { children: React.JSX.Element }) => {
  const token = localStorage.getItem('jwt');
  return token ? children : <Navigate to="/" replace />;
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
