class ClubItem {
  final int id;
  final String clubCode;
  final String clubName;
  final String description;
  final int slotLimit;
  final int registeredCount;
  final String startDate;
  final String endDate;
  final String status;
  final bool isRegistered;

  const ClubItem({
    required this.id,
    required this.clubCode,
    required this.clubName,
    required this.description,
    required this.slotLimit,
    required this.registeredCount,
    required this.startDate,
    required this.endDate,
    required this.status,
    required this.isRegistered,
  });

  factory ClubItem.fromJson(Map<String, dynamic> json) {
    return ClubItem(
      id: json['id'] is int ? json['id'] : int.tryParse('${json['id']}') ?? 0,
      clubCode: (json['clubCode'] ?? '').toString(),
      clubName: (json['clubName'] ?? '').toString(),
      description: (json['description'] ?? '').toString(),
      slotLimit: json['slotLimit'] is int
          ? json['slotLimit']
          : int.tryParse('${json['slotLimit']}') ?? 0,
      registeredCount: json['registeredCount'] is int
          ? json['registeredCount']
          : int.tryParse('${json['registeredCount']}') ?? 0,
      startDate: (json['startDate'] ?? '').toString(),
      endDate: (json['endDate'] ?? '').toString(),
      status: (json['status'] ?? '').toString(),
      isRegistered: json['isRegistered'] == true,
    );
  }
}