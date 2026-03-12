class ScoreItem {
  final String subjectName;
  final double? averageScore;
  final String result;

  const ScoreItem({
    required this.subjectName,
    required this.averageScore,
    required this.result,
  });

  factory ScoreItem.fromJson(Map<String, dynamic> json) {
    return ScoreItem(
      subjectName: (json['subjectName'] ?? '').toString(),
      averageScore: json['averageScore'] == null
          ? null
          : double.tryParse(json['averageScore'].toString()),
      result: (json['result'] ?? '').toString(),
    );
  }
}