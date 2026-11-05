import { useEffect, useState, type FormEvent } from 'react'
import { api, errorMessage } from '../api/client'
import type { ProfileUpdateResult, Role, UserProfile } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { PageTitle } from '../components/common'
import { useDialogs } from '../components/Dialogs'
import { EMPTY_PERSONAL, invalidPersonal, MIN_PASSWORD, PersonalFields, type PersonalData } from '../components/UserFields'

interface PasswordForm {
  currentPassword: string
  newPassword: string
  repeatPassword: string
}

const NO_PASSWORD: PasswordForm = { currentPassword: '', newPassword: '', repeatPassword: '' }

const ROLES: { role: Role; label: string }[] = [
  { role: 'Student', label: 'Usuário' },
  { role: 'Teacher', label: 'Professor' },
  { role: 'Administrator', label: 'Administrador' },
]

const toForm = (p: UserProfile): PersonalData => ({
  name: p.name ?? '',
  email: p.email ?? '',
  sex: p.sex,
  birthday: p.birthday?.slice(0, 10) ?? '',
  profession: p.profession ?? '',
  otherLanguages: p.otherLanguages ?? '',
  jlpt: p.jlpt,
})

/** Edição dos dados do usuário logado. Login e tipo de usuário só aparecem, não podem ser alterados. */
export function ProfilePage() {
  const { updateUser } = useAuth()
  const dialogs = useDialogs()
  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [form, setForm] = useState<PersonalData>(EMPTY_PERSONAL)
  const [password, setPassword] = useState<PasswordForm>(NO_PASSWORD)
  const [errors, setErrors] = useState<Set<string>>(new Set())
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    api<UserProfile>('/profile')
      .then((p) => {
        setProfile(p)
        setForm(toForm(p))
      })
      .catch((e) => setLoadError(errorMessage(e, 'Não foi possível carregar seus dados.')))
  }, [])

  const set = <K extends keyof PersonalData>(key: K, value: PersonalData[K]) => setForm((f) => ({ ...f, [key]: value }))
  const setPwd = (key: keyof PasswordForm, value: string) => setPassword((p) => ({ ...p, [key]: value }))
  const changesPassword = password.newPassword !== '' || password.repeatPassword !== ''

  async function submit(e: FormEvent) {
    e.preventDefault()
    const invalid = new Set<string>(invalidPersonal(form))
    if (changesPassword) {
      if (!password.currentPassword) invalid.add('currentPassword')
      if (password.newPassword.length < MIN_PASSWORD) invalid.add('newPassword')
      if (password.repeatPassword !== password.newPassword) invalid.add('repeatPassword')
    }
    setErrors(invalid)

    if (invalid.has('currentPassword')) await dialogs.alert('Informe a senha atual para trocar a senha.')
    else if (invalid.has('newPassword')) await dialogs.alert(`A senha deve ter pelo menos ${MIN_PASSWORD} dígitos.`)
    else if (invalid.has('repeatPassword')) await dialogs.alert('A repetição da senha não confere.')
    if (invalid.size > 0) return

    setBusy(true)
    try {
      const result = await api<ProfileUpdateResult>('/profile', {
        method: 'PUT',
        json: { ...form, birthday: form.birthday || null, ...(changesPassword ? password : {}) },
      })
      updateUser(result.user)
      setProfile({ ...profile!, ...form, sex: form.sex || profile!.sex, birthday: form.birthday || null })
      setPassword(NO_PASSWORD)
      await dialogs.info(result.message ?? 'Dados alterados com sucesso!')
    } catch (err) {
      dialogs.alert(errorMessage(err, 'Não foi possível salvar.'))
    } finally {
      setBusy(false)
    }
  }

  if (loadError) return <main className="narrow"><p className="notice error">{loadError}</p></main>
  if (!profile) return <main className="narrow"><p className="notice">Carregando…</p></main>

  const cls = (key: string) => (errors.has(key) ? 'is-wrong' : '')

  return (
    <main className="narrow">
      <PageTitle>Meus dados</PageTitle>
      <form className="card stack" onSubmit={submit} noValidate>
        <label>Login<input value={profile.login} disabled readOnly /></label>
        <fieldset disabled aria-describedby="role-hint">
          <legend>Tipo de usuário</legend>
          <div className="row" role="radiogroup">
            {ROLES.map(({ role, label }) => (
              <label key={role} className="inline">
                <input type="radio" name="role" checked={profile.role === role} readOnly /> {label}
              </label>
            ))}
          </div>
          <p id="role-hint" className="muted hint">Definido pelo administrador; não pode ser alterado aqui.</p>
        </fieldset>

        <PersonalFields value={form} onChange={set} errors={errors} />

        <fieldset className="stack">
          <legend>Alterar senha (opcional)</legend>
          <label>Senha atual<input className={cls('currentPassword')} type="password" value={password.currentPassword} onChange={(e) => setPwd('currentPassword', e.target.value)} autoComplete="current-password" /></label>
          <label>Nova senha<input className={cls('newPassword')} type="password" value={password.newPassword} onChange={(e) => setPwd('newPassword', e.target.value)} autoComplete="new-password" /></label>
          <label>Repetir nova senha<input className={cls('repeatPassword')} type="password" value={password.repeatPassword} onChange={(e) => setPwd('repeatPassword', e.target.value)} autoComplete="new-password" /></label>
        </fieldset>

        <div className="row">
          <button className="primary" disabled={busy}>{busy ? 'Salvando…' : 'Salvar'}</button>
          <button type="button" disabled={busy} onClick={() => { setForm(toForm(profile)); setPassword(NO_PASSWORD); setErrors(new Set()) }}>Desfazer alterações</button>
        </div>
      </form>
    </main>
  )
}
