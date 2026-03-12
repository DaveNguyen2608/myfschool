class ScoreSummary {
  final String academicYear;
  final String semester;
  final double averageScore;
  final String academicPerformance;
  final String conduct;
  final String note;

  const ScoreSummary({
    required this.academicYear,
    required this.semester,
    required this.averageScore,
    required this.academicPerformance,
    required this.conduct,
    required this.note,
  });

  factory ScoreSummary.fromJson(Map<String, dynamic> json) {
    return ScoreSummary(
      academicYear: (json['academicYear'] ?? '').toString(),
      semester: (json['semester'] ?? '').toString(),
      averageScore: double.tryParse('${json['averageScore']}') ?? 0,
      academicPerformance: (json['academicPerformance'] ?? '').toString(),
      conduct: (json['conduct'] ?? '').toString(),
      note: (json['note'] ?? '').toString(),
    );
  }
}