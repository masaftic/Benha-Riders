export interface Question {
  id: number;
  videoId: string;
  videoTitle: string;
  timestampSeconds: number;
  text: string;
  options: string[];
  correctAnswerIndex: number;
  explanation: string | null;
  pointsWorth: number;
  createdAt: string;
}

export interface CreateQuestionDto {
  videoId: string;
  timestampSeconds: number;
  text: string;
  options: string[];
  correctAnswerIndex: number;
  explanation?: string | null;
  pointsWorth: number;
}

export interface UpdateQuestionDto {
  timestampSeconds: number;
  text: string;
  options: string[];
  correctAnswerIndex: number;
  explanation?: string | null;
  pointsWorth: number;
}
