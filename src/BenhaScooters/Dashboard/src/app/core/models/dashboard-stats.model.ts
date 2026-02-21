export interface AdminDashboardStatsResponse {
  users: UserStats;
  content: ContentStats;
  engagement: EngagementStats;
  learning: LearningStats;
  guests: GuestStats;
  support: SupportStats;
  generatedAt: string;
}

export interface UserStats {
  totalParents: number;
  totalKids: number;
  newSignupsThisMonth: number;
}

export interface ContentStats {
  totalVideos: number;
  totalTags: number;
  videosVisibleToGuests: number;
  videosPlayableByGuests: number;
}

export interface EngagementStats {
  totalWatchTimeHours: number;
  totalVideoViews: number;
  completedVideos: number;
  completionRate: number;
  dailyActiveUsers: number;
  weeklyActiveUsers: number;
}

export interface LearningStats {
  totalQuestionsAnswered: number;
  correctAnswerRate: number;
  totalPointsEarned: number;
}

export interface GuestStats {
  activeGuestSessions: number;
  totalGuestSessionsAllTime: number;
}

export interface SupportStats {
  totalContactSubmissions: number;
  contactSubmissionsLast7Days: number;
  contactSubmissionsLast30Days: number;
}
