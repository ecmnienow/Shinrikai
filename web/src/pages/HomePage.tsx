import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { STUDENT_MENU, TEACHER_MENU } from '../menu'

/** Views/Home/Index.cshtml — página inicial com os mesmos atalhos do menu. */
export function HomePage() {
  const { user } = useAuth()
  const isEditor = user?.role === 'Administrator' || user?.role === 'Teacher'

  return (
    <main>
      <div className="hero">
        <img src="/logo.png" alt="" width={64} height={64} />
        <div>
          <h1>Shinrikai</h1>
          <p className="muted">Sistema hipermídia adaptativa para o estudo da língua japonesa</p>
        </div>
      </div>

      {!user && (
        <p className="notice">
          <Link to="/login">Entre</Link> ou cadastre-se para começar a estudar.
        </p>
      )}

      {user && (
        <>
          <h2>Aluno</h2>
          <div className="tiles">
            {STUDENT_MENU.flat().map((m) => (
              <Link key={m.to} to={m.to} className="tile">{m.label}</Link>
            ))}
          </div>
          {isEditor && (
            <>
              <h2>Professor</h2>
              <div className="tiles">
                {TEACHER_MENU.map((m) => (
                  <Link key={m.to} to={m.to} className="tile">{m.label}</Link>
                ))}
              </div>
            </>
          )}
        </>
      )}
    </main>
  )
}
