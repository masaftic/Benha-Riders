


export interface ParentSummary {
  id: string; // Guid
  fullName: string;
  email: string;
  numberOfKids: number;
  createdAt: string; // ISO date string
}

export interface KidSummary {
  id: string;
  name: string;
  age: number;
  avatarUrl: string;
  totalVideosCompleted: number;
  totalWatchTimeMinutes: number,
  totalQuestionsAnswered: number,
  totalCorrectAnswers: number,
  correctnessPercentage: number,
  totalPoints: number,
}

export interface ParentDetails {
  id: string;
  fullName: string;
  email: string;
  numberOfKids: number;
  createdAt: string;
  kids: KidSummary[];
}
