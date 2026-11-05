import type { Jlpt } from '../api/types'
import { LevelRadios } from './common'
import { useDialogs } from './Dialogs'

export const EMAIL = /^([\w-]+(?:\.[\w-]+)*)@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$/i
export const MIN_PASSWORD = 5

/** Dados pessoais comuns ao cadastro e à edição do perfil. */
export interface PersonalData {
  name: string
  email: string
  sex: '' | 'Male' | 'Female'
  birthday: string
  profession: string
  otherLanguages: string
  jlpt: Jlpt
}

export const EMPTY_PERSONAL: PersonalData = {
  name: '', email: '', sex: '', birthday: '', profession: '', otherLanguages: '', jlpt: 'None',
}

/** Campos obrigatórios inválidos (nome, email e sexo). */
export function invalidPersonal(data: PersonalData): Set<keyof PersonalData> {
  const invalid = new Set<keyof PersonalData>()
  if (!data.name.trim()) invalid.add('name')
  if (!EMAIL.test(data.email.trim())) invalid.add('email')
  if (!data.sex) invalid.add('sex')
  return invalid
}

export function PersonalFields({ value, onChange, errors }: {
  value: PersonalData
  onChange: <K extends keyof PersonalData>(key: K, value: PersonalData[K]) => void
  errors: ReadonlySet<string>
}) {
  const dialogs = useDialogs()
  const cls = (key: keyof PersonalData) => (errors.has(key) ? 'is-wrong' : '')

  return (
    <>
      <label>Nome/Sobrenome<input className={cls('name')} value={value.name} onChange={(e) => onChange('name', e.target.value)} autoComplete="name" /></label>
      <label>Email<input className={cls('email')} type="email" value={value.email} onChange={(e) => onChange('email', e.target.value)} autoComplete="email" /></label>
      <fieldset className={cls('sex')}>
        <legend>Sexo</legend>
        <div className="row">
          <label className="inline"><input type="radio" name="sex" checked={value.sex === 'Male'} onChange={() => onChange('sex', 'Male')} /> Masculino</label>
          <label className="inline"><input type="radio" name="sex" checked={value.sex === 'Female'} onChange={() => onChange('sex', 'Female')} /> Feminino</label>
        </div>
      </fieldset>
      <label>Data de nascimento<input type="date" value={value.birthday} onChange={(e) => onChange('birthday', e.target.value)} /></label>
      <label>Profissão<input value={value.profession} onChange={(e) => onChange('profession', e.target.value)} /></label>
      <label>Outras línguas<input value={value.otherLanguages} onChange={(e) => onChange('otherLanguages', e.target.value)} /></label>
      <fieldset>
        <legend>
          JLPT{' '}
          <button type="button" className="icon help" aria-label="O que é JLPT?" onClick={() => dialogs.info('JLPT trata-se do exame de proficiência para a língua japonesa.', 'Ajuda')}>?</button>
        </legend>
        <LevelRadios name="jlpt" value={value.jlpt} onChange={(l) => onChange('jlpt', l)} />
      </fieldset>
    </>
  )
}
