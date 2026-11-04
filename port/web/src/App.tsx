import type { ReactNode } from 'react'
import { BrowserRouter, Navigate, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth/AuthContext'
import { EditorPrototypePage } from './pages/EditorPrototypePage'
import { LoginPage } from './pages/LoginPage'
import { VocabularyPage } from './pages/VocabularyPage'

function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth()
  const location = useLocation()
  if (loading) return <p className="notice">Carregando…</p>
  if (!user) return <Navigate to="/login" state={{ from: location.pathname }} replace />
  return <>{children}</>
}

function Header() {
  const { user, logout } = useAuth()
  return (
    <header className="topbar">
      <NavLink to="/" className="brand">真理会 Shinrikai</NavLink>
      <nav>
        {user && <NavLink to="/vocabulario">Vocabulário</NavLink>}
        <NavLink to="/editor">Editor (protótipo)</NavLink>
      </nav>
      <span className="spacer" />
      {user ? (
        <>
          <span className="muted">{user.name} · {user.jlpt === 'None' ? 'sem nível' : user.jlpt}</span>
          <button type="button" onClick={logout}>Sair</button>
        </>
      ) : (
        <NavLink to="/login">Entrar</NavLink>
      )}
    </header>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Header />
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/vocabulario" element={<RequireAuth><VocabularyPage /></RequireAuth>} />
          {/* O protótipo do editor roda sem login/banco para validar o TipTap isoladamente. */}
          <Route path="/editor" element={<EditorPrototypePage />} />
          <Route path="*" element={<Navigate to="/vocabulario" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
