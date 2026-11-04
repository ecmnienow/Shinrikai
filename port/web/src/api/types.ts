export type Jlpt = 'None' | 'N5' | 'N4' | 'N3' | 'N2' | 'N1'
export type Role = 'Administrator' | 'Teacher' | 'Student'

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

export interface VocabularyItem {
  wordId: number
  prompt: string
}

export interface VocabularySession {
  subject: string
  toJapanese: boolean
  mistakes: boolean
  pages: VocabularyItem[][]
  totalItems: number
}

export interface VocabularyAnswer {
  wordId: number
  prompt: string
  answer: string
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
  subject1?: string
  subject2?: string
  subject3?: string
  word1?: string
  word2?: string
  word3?: string
  kanjiWord1?: string
  kanjiWord2?: string
  kanjiWord3?: string
}
