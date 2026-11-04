import { useEffect, useRef, useState, type ReactNode } from 'react'
import { BrowserRouter, Link, Navigate, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth/AuthContext'
import { DialogProvider } from './components/Dialogs'
import { STUDENT_MENU, TEACHER_MENU, type MenuItem } from './menu'
import { AboutPage } from './pages/AboutPage'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'

/** [Authorize] / [Authorize(Roles = "Administrator, Teacher")] do original. */
function RequireAuth({ children, editors }: { children: ReactNode; editors?: boolean }) {
  const { user, isEditor, loading } = useAuth()
  const location = useLocation()
  if (loading) return <p className="notice">Carregando…</p>
  if (!user) return <Navigate to="/login" state={{ from: location.pathname + location.search }} replace />
  if (editors && !isEditor) return <main><p className="notice error">Acesso restrito a professores e administradores.</p></main>
  return <>{children}</>
}

function Dropdown({ label, groups }: { label: string; groups: MenuItem[][] }) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  const location = useLocation()

  useEffect(() => setOpen(false), [location])
  useEffect(() => {
    const onClick = (e: MouseEvent) => ref.current && !ref.current.contains(e.target as Node) && setOpen(false)
    document.addEventListener('mousedown', onClick)
    return () => document.removeEventListener('mousedown', onClick)
  }, [])

  return (
    <div className="dropdown" ref={ref}>
      <button type="button" className="link" aria-expanded={open} aria-haspopup="true" onClick={() => setOpen((o) => !o)}>
        {label} ▾
      </button>
      {open && (
        <div className="dropdown-menu" role="menu">
          {groups.map((items, g) => (
            <div key={g} className="dropdown-group">
              {items.map((m) => (
                <NavLink key={m.to} to={m.to} role="menuitem">{m.label}</NavLink>
              ))}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function Header() {
  const { user, isEditor, logout } = useAuth()
  return (
    <header className="topbar">
      <Link to="/" className="brand">
        <img src="/logo.png" alt="" width={24} height={24} /> Shinrikai
      </Link>
      {user && (
        <nav className="menus" aria-label="Menu principal">
          <Dropdown label="Aluno" groups={STUDENT_MENU} />
          {isEditor && <Dropdown label="Professor" groups={[TEACHER_MENU]} />}
        </nav>
      )}
      <span className="spacer" />
      <NavLink to="/sobre">Sobre</NavLink>
      {user ? (
        <>
          <span className="muted user">{user.name} · {user.jlpt === 'None' ? 'sem nível' : user.jlpt}</span>
          <button type="button" className="link" onClick={logout}>Sair</button>
        </>
      ) : (
        <NavLink to="/login">Login</NavLink>
      )}
    </header>
  )
}

const routes = (items: MenuItem[], editors?: boolean) =>
  items.map(({ to, page: Page }) => <Route key={to} path={to} element={<RequireAuth editors={editors}><Page /></RequireAuth>} />)

export default function App() {
  return (
    <AuthProvider>
      <DialogProvider>
        <BrowserRouter>
          <Header />
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/sobre" element={<AboutPage />} />
            <Route path="/login" element={<LoginPage />} />
            {routes(STUDENT_MENU.flat())}
            {routes(TEACHER_MENU, true)}
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </DialogProvider>
    </AuthProvider>
  )
}
