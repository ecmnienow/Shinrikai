import { useEffect, useRef, useState, type ReactNode } from 'react'
import { BrowserRouter, Link, Navigate, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth/AuthContext'
import { DialogProvider } from './components/Dialogs'
import { STUDENT_MENU, TEACHER_MENU, type MenuItem } from './menu'
import { AboutPage } from './pages/AboutPage'
import { DictionaryPage } from './pages/DictionaryPage'
import { ExercisesPage } from './pages/ExercisesPage'
import { GrammarPage } from './pages/GrammarPage'
import { HomePage } from './pages/HomePage'
import { KanjiPage } from './pages/KanjiPage'
import { LoginPage } from './pages/LoginPage'
import { ExerciseRegisterPage } from './pages/register/ExerciseRegisterPage'
import { GrammarRegisterPage } from './pages/register/GrammarRegisterPage'
import { KanjiRegisterPage } from './pages/register/KanjiRegisterPage'
import { TextRegisterPage } from './pages/register/TextRegisterPage'
import { VocabularyRegisterPage } from './pages/register/VocabularyRegisterPage'
import { TextPage } from './pages/TextPage'
import { TranscriptionPage } from './pages/TranscriptionPage'
import { VocabularyPage } from './pages/VocabularyPage'

/** [Authorize] / [Authorize(Roles = "Administrator, Teacher")] do original. */
function RequireAuth({ children, editors }: { children: ReactNode; editors?: boolean }) {
  const { user, loading } = useAuth()
  const location = useLocation()
  if (loading) return <p className="notice">Carregando…</p>
  if (!user) return <Navigate to="/login" state={{ from: location.pathname + location.search }} replace />
  if (editors && user.role !== 'Administrator' && user.role !== 'Teacher')
    return <main><p className="notice error">Acesso restrito a professores e administradores.</p></main>
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
  const { user, logout } = useAuth()
  const isEditor = user?.role === 'Administrator' || user?.role === 'Teacher'
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

const auth = (page: ReactNode) => <RequireAuth>{page}</RequireAuth>
const editors = (page: ReactNode) => <RequireAuth editors>{page}</RequireAuth>

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

            {/* Aluno */}
            <Route path="/transcricao" element={auth(<TranscriptionPage />)} />
            <Route path="/vocabulario" element={auth(<VocabularyPage />)} />
            <Route path="/kanjis" element={auth(<KanjiPage />)} />
            <Route path="/gramatica" element={auth(<GrammarPage />)} />
            <Route path="/textos" element={auth(<TextPage />)} />
            <Route path="/exercicios" element={auth(<ExercisesPage />)} />
            <Route path="/dicionario" element={auth(<DictionaryPage />)} />

            {/* Professor */}
            <Route path="/professor/gramatica" element={editors(<GrammarRegisterPage />)} />
            <Route path="/professor/vocabulario" element={editors(<VocabularyRegisterPage />)} />
            <Route path="/professor/kanjis" element={editors(<KanjiRegisterPage />)} />
            <Route path="/professor/textos" element={editors(<TextRegisterPage />)} />
            <Route path="/professor/exercicios" element={editors(<ExerciseRegisterPage />)} />

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </DialogProvider>
    </AuthProvider>
  )
}
