import type { ComponentType } from 'react'
import { DictionaryPage } from './pages/DictionaryPage'
import { ExercisesPage } from './pages/ExercisesPage'
import { GrammarPage } from './pages/GrammarPage'
import { KanjiPage } from './pages/KanjiPage'
import { ExerciseRegisterPage } from './pages/register/ExerciseRegisterPage'
import { GrammarRegisterPage } from './pages/register/GrammarRegisterPage'
import { KanjiRegisterPage } from './pages/register/KanjiRegisterPage'
import { SubjectRegisterPage } from './pages/register/SubjectRegisterPage'
import { TextRegisterPage } from './pages/register/TextRegisterPage'
import { VocabularyRegisterPage } from './pages/register/VocabularyRegisterPage'
import { TextPage } from './pages/TextPage'
import { TranscriptionPage } from './pages/TranscriptionPage'
import { VocabularyPage } from './pages/VocabularyPage'

/** Menus do _Layout.cshtml original ("Aluno" com separadores e "Professor"); também geram as rotas. */
export interface MenuItem {
  to: string
  label: string
  page: ComponentType
}

export const STUDENT_MENU: MenuItem[][] = [
  [
    { to: '/transcricao', label: 'Transcrição para hiragana', page: TranscriptionPage },
    { to: '/vocabulario', label: 'Estudo de vocabulário', page: VocabularyPage },
    { to: '/kanjis', label: 'Leitura de kanjis', page: KanjiPage },
  ],
  [
    { to: '/gramatica', label: 'Estudo de gramática', page: GrammarPage },
    { to: '/textos', label: 'Leitura de textos', page: TextPage },
    { to: '/exercicios', label: 'Resolução de exercícios', page: ExercisesPage },
  ],
  [{ to: '/dicionario', label: 'Dicionário', page: DictionaryPage }],
]

export const TEACHER_MENU: MenuItem[] = [
  { to: '/professor/gramatica', label: 'Cadastro de gramática', page: GrammarRegisterPage },
  { to: '/professor/vocabulario', label: 'Cadastro de vocabulário', page: VocabularyRegisterPage },
  { to: '/professor/assuntos', label: 'Cadastro de assuntos', page: SubjectRegisterPage },
  { to: '/professor/kanjis', label: 'Cadastro de kanjis', page: KanjiRegisterPage },
  { to: '/professor/textos', label: 'Cadastro de textos', page: TextRegisterPage },
  { to: '/professor/exercicios', label: 'Cadastro de exercícios', page: ExerciseRegisterPage },
]
