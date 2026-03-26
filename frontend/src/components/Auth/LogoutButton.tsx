import { useNavigate } from 'react-router-dom';

export default function LogoutButton() {
  const navigate = useNavigate();

  const handleLogout = () => {
    localStorage.removeItem('jwt'); 
    navigate('/'); 
  };

  return (
    <button onClick={handleLogout}>
      Logout
    </button>
  );
}