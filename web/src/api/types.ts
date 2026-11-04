export type Jlpt = 'None' | 'N5' | 'N4' | 'N3' | 'N2' | 'N1'
export type Role = 'Administrator' | 'Teacher' | 'Student'

export const LEVELS: Jlpt[] = ['N5', 'N4', 'N3', 'N2', 'N1']

export interface UserInfo {
  id: number
  login: string
  name: string
  jlpt: Jlpt
  role: Role
}

export interface AuthenticatedUser {
  token: string
  expiresAt: string
  user: UserInfo
}

export interface Response {
  ok: boolean
  message?: string
}

export interface Subject {
  id: number
  name: string
}

// ---- Vocabulário / Transcrição ----

export interface VocabularyItem {
  wordId: number
  prompt: string
}

export interface VocabularyResult {
  wordId: number
  prompt: string
  answer: string | null
  wrong: boolean
  expected: string[]
}

export interface Word {
  id: number
  kana: string
  romaji: string
  subjectID1: number
  subjectID2: number
  subjectID3: number
  subject1?: string | null
  subject2?: string | null
  subject3?: string | null
  word1?: string | null
  word2?: string | null
  word3?: string | null
  kanjiWord1?: string | null
  kanjiWord2?: string | null
  kanjiWord3?: string | null
  hasImage1: boolean
  hasImage2: boolean
  hasImage3: boolean
  hasAudio1: boolean
  hasAudio2: boolean
  hasAudio3: boolean
}

// ---- Kanji ----

export interface KanjiItem {
  kanjiId: number
  kanji: string
  inputs: number
}

export interface KanjiResult {
  kanjiId: number
  kanji: string
  answers: (string | null)[]
  wrongInputs: boolean[]
  wrong: boolean
  expected: string[]
}

export interface Kanji {
  id: number
  kanjiLetter: string
  kana: string
  romaji: string
  info: string | null
  jlpt: string | null
  hasImage: boolean
}

// ---- Gramática / Texto ----

export interface ContentListItem {
  id: number
  title: string
  level: Jlpt
  hasEnoughLevel: boolean
  hasExercises: boolean
}

export interface ContentGroup {
  level: Jlpt
  items: ContentListItem[]
}

export interface Grammar {
  index: number
  title: string
  level: Jlpt
  body: string
  requisites: number[]
}

export interface GrammarRequisites {
  levelTooHigh: boolean
  message: string
  requisites: string[]
}

export interface GrammarSummary {
  id: number
  title: string
}

// ---- Exercícios ----

export interface ExerciseQuestion {
  id: number
  question: string
  options: { value: number; text: string }[]
}

export interface Exercise {
  id: number
  grammarId: number
  textId: number
  question: string
  option1: string
  option2: string
  option3: string
  option4: string
  rightAnswer: number
  level: Jlpt
}

// ---- Dicionário ----

export type SearchType = 'General' | 'Japanese' | 'Portuguese'

export interface SearchResult {
  term: string
  results: { foundTerm: string; word: Word }[]
}
