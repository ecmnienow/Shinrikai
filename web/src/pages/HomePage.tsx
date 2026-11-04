import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { STUDENT_MENU, TEACHER_MENU, type MenuItem } from '../menu'

function Tiles({ title, items }: { title: string; items: MenuItem[] }) {
  return (
    <>
      <h2>{title}</h2>
      <div className="tiles">
        {items.map((m) => (
          <Link key={m.to} to={m.to} className="tile">{m.label}</Link>
        ))}
      </div>
    </>
  )
}

/** Views/Home/Index.cshtml — página inicial com os mesmos atalhos do menu. */
export function HomePage() {
  const { user, isEditor } = useAuth()

  return (
    <main>
      <div className="hero">
        <img src="/logo.png" alt="" width={64} height={64} />
        <div>
          <h1>Shinrikai</h1>
          <p className="muted">Sistema hipermídia adaptativa para o estudo da língua japonesa</p>
        </div>
      </div>

      {user ? (
        <>
          <Tiles title="Aluno" items={STUDENT_MENU.flat()} />
          {isEditor && <Tiles title="Professor" items={TEACHER_MENU} />}
        </>
      ) : (
        <p className="notice">
          <Link to="/login">Entre</Link> ou cadastre-se para começar a estudar.
        </p>
      )}
    </main>
  )
}
