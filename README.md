# Shortest path test

## ��������� �������:
{���� �� �������� �����} {��������� ���������} {�������� ���������} {����� ������ ���� hh:mm}


#### ������:
```
input.txt 1 12 13:45 
```

## ������:
���� N ���������, ������ ������� ����� �� ������� ���������� ������������ ��������. �������� ��������� ������� (������������ ��� ����� � �������) � ����� �������� ����� �����������. �������� ������� �� ������� (���������� �� ������ ���������) � ������������ �����.
���������� �������� ���������, ������� ����� �������� ����� ������� � ����� ������� ����. ��������� ��������� ������ ��������� ��������� ���� � ���������� ���������, �������� ��������� � �������� ����� � ����� ����������� �� ��������� �����.
#### �����:
1. ��������� ������������� ������ �� 1 �� N.
2. ����� ���� ����� ����������� �������� � ������� ����� ������.
3. ��������� ������� �������� � ������ ����� ������.
4. �������� ���� ����� �� ������.
5. ������� �� ������ ����� �� ��������� (����� 0 �����).
6. ������� ���� �� �������� ������.
7. ��� �������� ��������� � 00:00, �.�. ��� ������� �������� �� ��������.

#### ������ �������� �����:
{����� ���������}

{����� ���������}

{����� ����������� 1 ��������} {����� ����������� 2 ��������} ... {����� ����������� N ��������}

{��������� ������� �� 1 ��������} {��������� ������� �� 2 ��������} ... {��������� ������� �� N ��������}

{����� ��������� �� �������� 1 ��������} {����� 1 ���������} {����� 2 ���������} ... {����� ��������� ���������} {����� � ���� ����� 1 � 2 ����������} {����� � ���� ����� 2 � 3 ����������} ... {����� � ���� ����� X � 1 ����������}
... 
�������� ��������� ��������� ...

#### ������:
```
2

4

10:00 12:00

10 20

2 1 3 5 7

3 1 2 4 10 5 20

```