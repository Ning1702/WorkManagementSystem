import { createContext, useContext, useState } from 'react'

const AuthContext = createContext()

// Giải mã JWT để lấy role
function parseRole(token) {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    // ASP.NET Core JWT dùng claim type dài
    return (
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
      payload['role'] ||
      'User'
    )
  } catch {
    return 'User'
  }
}

export function AuthProvider({ children }) {
  const [token, setToken] = useState(localStorage.getItem('token') || null)
  const [role, setRole] = useState(() => {
    const t = localStorage.getItem('token')
    return t ? parseRole(t) : null
  })

  const login = (newToken) => {
    localStorage.setItem('token', newToken)
    setToken(newToken)
    setRole(parseRole(newToken))
  }

  const logout = () => {
    localStorage.removeItem('token')
    setToken(null)
    setRole(null)
  }

  return (
    <AuthContext.Provider value={{ token, role, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  return useContext(AuthContext)
}
