class ScheduleItem {
  final int dayOfWeek;
  final int periodNo;
  final String startTime;
  final String endTime;
  final String subjectName;
  final String roomName;
  final String teacherName;
  final String note;

  ScheduleItem({
    required this.dayOfWeek,
    required this.periodNo,
    required this.startTime,
    required this.endTime,
    required this.subjectName,
    required this.roomName,
    required this.teacherName,
    required this.note,
  });

  factory ScheduleItem.fromJson(Map<String, dynamic> json) {
    return ScheduleItem(
      dayOfWeek: json['dayOfWeek'] ?? 0,
      periodNo: json['periodNo'] ?? 0,
      startTime: json['startTime']?.toString() ?? '',
      endTime: json['endTime']?.toString() ?? '',
      subjectName: json['subjectName']?.toString() ?? '',
      roomName: json['roomName']?.toString() ?? '',
      teacherName: json['teacherName']?.toString() ?? '',
      note: json['note']?.toString() ?? '',
    );
  }
}