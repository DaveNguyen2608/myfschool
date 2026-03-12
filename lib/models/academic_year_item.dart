class AcademicYearItem {
  final int id;
  final String name;

  const AcademicYearItem({
    required this.id,
    required this.name,
  });

  factory AcademicYearItem.fromJson(Map<String, dynamic> json) {
    return AcademicYearItem(
      id: json['id'] is int ? json['id'] : int.tryParse('${json['id']}') ?? 0,
      name: (json['name'] ?? '').toString(),
    );
  }
}