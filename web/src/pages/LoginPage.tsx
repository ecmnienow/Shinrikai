import { useState, type FormEvent } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { errorMessage, post } from '../api/client'
import type { Jlpt, Response } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { LevelRadios } from '../components/common'
import { useDialogs } from '../components/Dialogs'

const EMAIL = /^([\w-]+(?:\.[\w-]+)*)@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$/i
const MIN_PASSWORD = 5

interface SignUpForm {
  name: string
  email: string
  login: string
  password: string
  repeatPassword: string
  sex: '' | 'Male' | 'Female'
  birthday: string
  profession: string
  otherLanguages: string
  jlpt: Jlpt
}

const EMPTY: SignUpForm = {
  name: '', email: '', login: '', password: '', repeatPassword: '', sex: '',
  birthday: '', profession: '', otherLanguages: '', jlpt: 'None',
}

/** Port de Views/Login/Index.cshtml: login e "Cadastre-se" na mesma tela. */
export function LoginPage() {
  const { user, login } = useAuth()
  const dialogs = useDialogs()
  const location = useLocation()
  const [mode, setMode] = useState<'login' | 'signup'>('login')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [wrong, setWrong] = useState(false)
  const [busy, setBusy] = useState(false)
  const [form, setForm] = useState<SignUpForm>(EMPTY)
  const [errors, setErrors] = useState<Set<keyof SignUpForm>>(new Set())

  if (user) {
    const from = (location.state as { from?: string } | null)?.from ?? '/'
    return <Navigate to={from} replace />
  }

  async function submitLogin(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    try {
      await login(username, password)
    } catch {
      setWrong(true)
      setPassword('')
    } finally {
      setBusy(false)
    }
  }

  const set = <K extends keyof SignUpForm>(key: K, value: SignUpForm[K]) => setForm((f) => ({ ...f, [key]: value }))

  async function submitSignUp(e: FormEvent) {
    e.preventDefault()
    const invalid = new Set<keyof SignUpForm>()
    if (!form.name.trim()) invalid.add('name')
    if (!EMAIL.test(form.email)) invalid.add('email')
    if (!form.login.trim()) invalid.add('login')
    if (form.password.length < MIN_PASSWORD) invalid.add('password')
    if (!form.repeatPassword || form.repeatPassword !== form.password) invalid.add('repeatPassword')
    if (!form.sex) invalid.add('sex')
    setErrors(invalid)

    if (invalid.has('password')) await dialogs.alert(`A senha deve ter pelo menos ${MIN_PASSWORD} dígitos.`)
    else if (invalid.has('repeatPassword')) await dialogs.alert('A repetição da senha não confere.')
    if (invalid.size > 0) return

    setBusy(true)
    try {
      const response = await post<Response>('/auth/signup', { ...form, birthday: form.birthday || '0001-01-01' })
      await dialogs.info(response.message ?? 'Usuário cadastrado com sucesso!')
      setUsername(form.login)
      setForm(EMPTY)
      setMode('login')
    } catch (err) {
      dialogs.alert(errorMessage(err, 'Não foi possível cadastrar.'))
    } finally {
      setBusy(false)
    }
  }

  const cls = (key: keyof SignUpForm) => (errors.has(key) ? 'is-wrong' : '')

  return (
    <main className="narrow">
      <h1>Shinrikai</h1>
      <p className="muted">Sistema hipermídia adaptativa para o estudo da língua japonesa</p>

      {mode === 'login' ? (
        <form className="card stack" onSubmit={submitLogin}>
          <h2>Entrar</h2>
          <label>
            Usuário
            <input className={wrong && !username ? 'is-wrong' : ''} value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="username" autoFocus required />
          </label>
          <label>
            Senha
            <input className={wrong ? 'is-wrong' : ''} type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" required />
          </label>
          {wrong && <p className="error">Usuário ou senha inválidos.</p>}
          <div className="row">
            <button className="primary" disabled={busy}>{busy ? 'Entrando…' : 'Logar'}</button>
            <span className="spacer" />
            <span className="muted">Não tem conta?</span>
            <button type="button" onClick={() => setMode('signup')}>Cadastre-se</button>
          </div>
        </form>
      ) : (
        <form className="card stack" onSubmit={submitSignUp} noValidate>
          <h2>Cadastro</h2>
          <label>Nome/Sobrenome<input className={cls('name')} value={form.name} onChange={(e) => set('name', e.target.value)} autoComplete="name" /></label>
          <label>Email<input className={cls('email')} type="email" value={form.email} onChange={(e) => set('email', e.target.value)} autoComplete="email" /></label>
          <label>Login<input className={cls('login')} value={form.login} onChange={(e) => set('login', e.target.value)} autoComplete="username" /></label>
          <label>Senha<input className={cls('password')} type="password" value={form.password} onChange={(e) => set('password', e.target.value)} autoComplete="new-password" /></label>
          <label>Repetir senha<input className={cls('repeatPassword')} type="password" value={form.repeatPassword} onChange={(e) => set('repeatPassword', e.target.value)} autoComplete="new-password" /></label>
          <fieldset className={cls('sex')}>
            <legend>Sexo</legend>
            <div className="row">
              <label className="inline"><input type="radio" name="sex" checked={form.sex === 'Male'} onChange={() => set('sex', 'Male')} /> Masculino</label>
              <label className="inline"><input type="radio" name="sex" checked={form.sex === 'Female'} onChange={() => set('sex', 'Female')} /> Feminino</label>
            </div>
          </fieldset>
          <label>Data de nascimento<input type="date" value={form.birthday} onChange={(e) => set('birthday', e.target.value)} /></label>
          <label>Profissão<input value={form.profession} onChange={(e) => set('profession', e.target.value)} /></label>
          <label>Outras línguas<input value={form.otherLanguages} onChange={(e) => set('otherLanguages', e.target.value)} /></label>
          <fieldset>
            <legend>
              JLPT{' '}
              <button type="button" className="icon help" aria-label="O que é JLPT?" onClick={() => dialogs.info('JLPT trata-se do exame de proficiência para a língua japonesa.', 'Ajuda')}>?</button>
            </legend>
            <LevelRadios name="jlpt" value={form.jlpt} onChange={(l) => set('jlpt', l)} />
          </fieldset>
          <div className="row">
            <button className="primary" disabled={busy}>Cadastrar</button>
            <button type="button" onClick={() => { setMode('login'); setErrors(new Set()) }}>Cancelar</button>
          </div>
        </form>
      )}
    </main>
  )
}
