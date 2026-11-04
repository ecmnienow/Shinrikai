/** Menus do _Layout.cshtml original ("Aluno" com separadores e "Professor"). */
export interface MenuItem {
  to: string
  label: string
}

export const STUDENT_MENU: MenuItem[][] = [
  [
    { to: '/transcricao', label: 'Transcrição para hiragana' },
    { to: '/vocabulario', label: 'Estudo de vocabulário' },
    { to: '/kanjis', label: 'Leitura de kanjis' },
  ],
  [
    { to: '/gramatica', label: 'Estudo de gramática' },
    { to: '/textos', label: 'Leitura de textos' },
    { to: '/exercicios', label: 'Resolução de exercícios' },
  ],
  [{ to: '/dicionario', label: 'Dicionário' }],
]

export const TEACHER_MENU: MenuItem[] = [
  { to: '/professor/gramatica', label: 'Cadastro de gramática' },
  { to: '/professor/vocabulario', label: 'Cadastro de vocabulário' },
  { to: '/professor/kanjis', label: 'Cadastro de kanjis' },
  { to: '/professor/textos', label: 'Cadastro de textos' },
  { to: '/professor/exercicios', label: 'Cadastro de exercícios' },
]
