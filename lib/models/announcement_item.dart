class AnnouncementItem {
  final int id;
  final String title;
  final String? description;
  final String? imageUrl;
  final String? startDate;
  final String? endDate;

  AnnouncementItem({
    required this.id,
    required this.title,
    this.description,
    this.imageUrl,
    this.startDate,
    this.endDate,
  });

  factory AnnouncementItem.fromJson(Map<String, dynamic> json) {
    return AnnouncementItem(
      id: json['id'] is int ? json['id'] : int.parse(json['id'].toString()),
      title: json['title']?.toString() ?? '',
      description: json['description']?.toString(),
      imageUrl: json['imageUrl']?.toString(),
      startDate: json['startDate']?.toString(),
      endDate: json['endDate']?.toString(),
    );
  }
}