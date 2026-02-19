import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import Login from './components/Login'
import Dashboard from './components/Dashboard'
import DriverDetails from './components/DriverDetails'

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem('accessToken')
    setIsAuthenticated(!!token)
    setIsLoading(false)
  }, [])

  const handleLogin = (accessToken: string, refreshToken: string) => {
    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem('refreshToken', refreshToken)
    setIsAuthenticated(true)
  }

  const handleLogout = () => {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    setIsAuthenticated(false)
  }

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-100">
        <div className="text-xl text-gray-600">Loading...</div>
      </div>
    )
  }

  return (
    <Router>
      <Routes>
        <Route 
          path="/login" 
          element={
            isAuthenticated ? 
            <Navigate to="/" replace /> : 
            <Login onLogin={handleLogin} />
          } 
        />
        <Route 
          path="/" 
          element={
            isAuthenticated ? 
            <Dashboard onLogout={handleLogout} /> : 
            <Navigate to="/login" replace />
          } 
        />
        <Route 
          path="/drivers/:driverId" 
          element={
            isAuthenticated ? 
            <DriverDetails onLogout={handleLogout} /> : 
            <Navigate to="/login" replace />
          } 
        />
      </Routes>
    </Router>
  )
}

export default App
