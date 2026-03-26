import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';
import type { UserLoginDto, LoginResponseDto } from '../../types';
import LogoutButton from './LogoutButton';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async (e: React.SubmitEvent) => {
    e.preventDefault();
    const dto: UserLoginDto = { email, password };
    console.log("UserLoginDto: ", dto)
    try {
      const res = await api.post<LoginResponseDto>('/users/login', dto);
      localStorage.setItem('jwt', res.data.token);
      navigate('/applications');
    } catch (err) {
      console.error(err);
      setError('Login failed');
    }
  };

  return (
    <div>
      <h2>Login</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      <form onSubmit={handleSubmit}>
        <input
          type="email"
          placeholder="Email"
          value={email}
          onChange={e => setEmail(e.target.value)}
        /><br/>
        <input
          type="password"
          placeholder="Password"
          value={password}
          onChange={e => setPassword(e.target.value)}
        /><br/>
        <button type="submit">Login</button>
      </form>

      <div>
        <LogoutButton />
    </div>
    </div>
  );
}